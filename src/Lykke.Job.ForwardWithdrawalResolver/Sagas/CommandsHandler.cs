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
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.Service.Assets.Client;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.ExchangeOperations.Client.Models;

namespace Lykke.Job.ForwardWithdrawalResolver.Sagas
{
    public class CommandsHandler
    {
        private readonly IExchangeOperationsServiceClient _exchangeOperationsService;
        private readonly string _hotWalletId;
        private readonly ILog _log;
        private readonly IForwardWithdrawalRepository _repository;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;

        public CommandsHandler(ILogFactory logFactory,
            IForwardWithdrawalRepository repository,
            IExchangeOperationsServiceClient exchangeOperationsService,
            string hotWalletId,
            IAssetsServiceWithCache assetsServiceWithCache)
        {
            _log = logFactory.CreateLog(this);
            _repository = repository;
            _exchangeOperationsService = exchangeOperationsService;
            _hotWalletId = hotWalletId;
            _assetsServiceWithCache = assetsServiceWithCache;
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
                var asset = await _assetsServiceWithCache.TryGetAssetAsync(command.AssetId);

                var result = await _exchangeOperationsService.ExchangeOperations.TransferAsync(
                    new TransferRequestModel
                    {
                        DestClientId = command.ClientId,
                        SourceClientId = _hotWalletId,
                        Amount = command.Amount.TruncateDecimalPlaces(asset.Accuracy),
                        AssetId = command.AssetId,
                        TransferTypeCode = "Common",
                        OperationId = command.NewCashinId.ToString(),
                    });

                if (result.IsOk())
                {
                    _log.Info($"Done processing: {command.ToJson()}");

                    eventPublisher.PublishEvent(new CashInProcesedEvent
                    {
                        ClientId = command.ClientId,
                        OperationId = command.CashinId
                    });

                    return CommandHandlingResult.Ok();
                }

                if (result.Code == (int)MeStatusCodes.Duplicate)
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