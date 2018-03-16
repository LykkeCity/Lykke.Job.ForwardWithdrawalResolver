using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.ForwardWithdrawalResolver.AzureRepositories;
using Lykke.Job.ForwardWithdrawalResolver.Core.Services;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Commands;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Events;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.Service.ExchangeOperations.Client;

namespace Lykke.Job.ForwardWithdrawalResolver.Sagas
{
    public class CommandsHandler
    {
        private readonly ILog _log;
        private readonly IForwardWithdrawalRepository _repository;
        private readonly IExchangeOperationsServiceClient _exchangeOperationsService;
        private readonly IPaymentResolver _paymentResolver;
        private readonly string _hotWalletId;

        public CommandsHandler(ILog log,
            IForwardWithdrawalRepository repository,
            IExchangeOperationsServiceClient exchangeOperationsService,
            IPaymentResolver paymentResolver,
            string hotWalletId)
        {
            _log = log;
            _repository = repository;
            _exchangeOperationsService = exchangeOperationsService;
            _paymentResolver = paymentResolver;
            _hotWalletId = hotWalletId;
        }
        
        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(RemoveEntryCommand command, IEventPublisher eventPublisher)
        {
            _log.WriteInfo(nameof(RemoveEntryCommand), command.ClientId, $"Beginning to process: {command.ToJson()}");

            var forwardWithdrawal = await _repository.TryGetAsync(command.ClientId, command.Id);

            if (forwardWithdrawal == null)
            {
                return CommandHandlingResult.Ok();
            }
            
            var entryExisted = await _repository.DeleteIfExistsAsync(command.ClientId, command.Id);
            
            if (entryExisted)
            {
                eventPublisher.PublishEvent(new PaymentEntryRemovedEvent
                {
                    Id = forwardWithdrawal.Id,
                    ClientId = forwardWithdrawal.ClientId,
                    AssetId = forwardWithdrawal.AssetId,
                    Amount = forwardWithdrawal.Amount
                });
            }
            
            return CommandHandlingResult.Ok();
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(ResolvePaymentCommand command, IEventPublisher eventPublisher)
        {
            _log.WriteInfo(nameof(ResolvePaymentCommand), command.ClientId, $"Beginning to process: {command.ToJson()}");

            var assetToPayId = await _paymentResolver.Resolve(command.AssetId);
            
            eventPublisher.PublishEvent(new PaymentResolvedEvent
            {
                Id = command.Id,
                ClientId = command.ClientId,
                AssetId = assetToPayId,
                Amount = command.Amount
            });
            
            return CommandHandlingResult.Ok();
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(ProcessPaymentCommand command, IEventPublisher eventPublisher)
        {
            _log.WriteInfo(nameof(ProcessPaymentCommand), command.ClientId, $"Beginning to process: {command.ToJson()}");

            try
            {
                var result = await _exchangeOperationsService.TransferAsync(
                    command.ClientId,
                    _hotWalletId,
                    command.Amount,
                    command.AssetId,
                    "Common",
                    null,
                    null,
                    command.Id);

                if (result.IsOk())
                {
                    _log.WriteInfo(nameof(ProcessPaymentCommand), command.ClientId, $"Done processing: {command.ToJson()}");
                    
                    return CommandHandlingResult.Ok();
                }
                else
                {
                    if (result.Code == (int)MeStatusCodes.Duplicate)
                    {
                        _log.WriteWarning(nameof(ProcessPaymentCommand), command.ClientId, $"Duplicate transfer attempt: {command.ToJson()}");
                        
                        return CommandHandlingResult.Ok();
                    }
                    else
                    {
                        throw new InvalidOperationException($"During transfer of {command.Id}, ME responded with: {result.Code}");
                    }
                }
            }
            catch (Exception e)
            {
                _log.WriteError(nameof(ProcessPaymentCommand), command.ClientId, e);
                
                return CommandHandlingResult.Fail(TimeSpan.FromMinutes(1));
            }
        }
    }
}
