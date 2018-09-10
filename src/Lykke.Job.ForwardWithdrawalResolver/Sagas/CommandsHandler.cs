using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.ForwardWithdrawalResolver.AzureRepositories;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Commands;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Events;
using Lykke.Job.OperationsCache.Client;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.OperationsHistory.Client;

namespace Lykke.Job.ForwardWithdrawalResolver.Sagas
{
    public class CommandsHandler
    {
        private readonly IExchangeOperationsServiceClient _exchangeOperationsService;
        private readonly string _hotWalletId;
        private readonly ILog _log;
        private readonly IOperationsCacheClient _operationsCacheClient;
        private readonly IOperationsHistoryClient _operationsHistoryClient;
        private readonly IForwardWithdrawalRepository _repository;

        public CommandsHandler(ILogFactory logFactory,
            IForwardWithdrawalRepository repository,
            IExchangeOperationsServiceClient exchangeOperationsService,
            IOperationsCacheClient operationsCacheClient,
            IOperationsHistoryClient operationsHistoryClient,
            string hotWalletId)
        {
            _log = logFactory.CreateLog(this);
            _repository = repository;
            _exchangeOperationsService = exchangeOperationsService;
            _operationsHistoryClient = operationsHistoryClient;
            _operationsCacheClient = operationsCacheClient;
            _hotWalletId = hotWalletId;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(RemoveEntryCommand command, IEventPublisher eventPublisher)
        {
            var forwardWithdrawal = await _repository.TryGetAsync(command.ClientId, command.Id);

            if (forwardWithdrawal == null)
                return CommandHandlingResult.Ok();

            var entryExisted = await _repository.DeleteIfExistsAsync(command.ClientId, command.Id);

            if (entryExisted)
                eventPublisher.PublishEvent(new PaymentEntryRemovedEvent
                {
                    Id = forwardWithdrawal.Id,
                    ClientId = forwardWithdrawal.ClientId,
                    AssetId = forwardWithdrawal.AssetId,
                    Amount = forwardWithdrawal.Amount,
                    CashInId = forwardWithdrawal.CashInId
                });

            return CommandHandlingResult.Ok();
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(RemoveEntryFromHistoryServiceCommand command,
            IEventPublisher eventPublisher)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(command.CashInId))
                    await _operationsHistoryClient.DeleteByClientIdOperationId(command.ClientId, command.CashInId);
                else
                    _log.Warning($"CashInId absent: {command.ToJson()}");

                eventPublisher.PublishEvent(new CashInRemovedFromHistoryServiceEvent
                {
                    Id = command.Id,
                    ClientId = command.ClientId,
                    AssetId = command.AssetId,
                    Amount = command.Amount,
                    CashInId = command.CashInId
                });

                return CommandHandlingResult.Ok();
            }
            catch (Exception e)
            {
                _log.Error(e, context: command.ClientId);

                return CommandHandlingResult.Fail(TimeSpan.FromSeconds(30));
            }
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(RemoveEntryFromHistoryJobCommand command,
            IEventPublisher eventPublisher)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(command.CashInId))
                    await _operationsCacheClient.RemoveCashInIfExists(command.ClientId, command.CashInId);
                else
                    _log.Warning($"CashInId absent: {command.ToJson()}");

                eventPublisher.PublishEvent(new CashInRemovedFromHistoryJobEvent
                {
                    Id = command.Id,
                    ClientId = command.ClientId,
                    AssetId = command.AssetId,
                    Amount = command.Amount,
                    CashInId = command.CashInId
                });

                return CommandHandlingResult.Ok();
            }
            catch (Exception e)
            {
                _log.Error(e, context: command.ClientId);

                return CommandHandlingResult.Fail(TimeSpan.FromSeconds(30));
            }
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(ProcessPaymentCommand command, IEventPublisher eventPublisher)
        {
            try
            {
                var result = await _exchangeOperationsService.TransferAsync(
                    command.ClientId,
                    _hotWalletId,
                    command.Amount,
                    command.AssetId,
                    "Common",
                    transactionId: command.Id);

                if (result.IsOk())
                {
                    _log.Info($"Done processing: {command.ToJson()}");

                    eventPublisher.PublishEvent(new CashInProcesedEvent
                    {
                        ClientId = command.ClientId,
                        OperationId = command.Id
                    });

                    return CommandHandlingResult.Ok();
                }

                if (result.Code == (int) MeStatusCodes.Duplicate)
                {
                    _log.Warning($"Duplicate transfer attempt: {command.ToJson()}");

                    return CommandHandlingResult.Ok();
                }

                throw new InvalidOperationException(
                    $"During transfer of {command.Id}, ME responded with: {result.Code}");
            }
            catch (Exception e)
            {
                _log.Error(e, context: command.ClientId);

                return CommandHandlingResult.Fail(TimeSpan.FromMinutes(1));
            }
        }
    }
}