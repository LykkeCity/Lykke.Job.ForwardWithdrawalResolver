using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Commands;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Events;

namespace Lykke.Job.ForwardWithdrawalResolver.Sagas
{
    [UsedImplicitly]
    public class PaymentSaga
    {
        private readonly ILog _log;

        public PaymentSaga(ILog log)
        {
            _log = log;
        }
        
        [UsedImplicitly]
        public async Task Handle(PaymentEntryRemovedEvent evt, ICommandSender commandSender)
        {
            _log.WriteInfo(nameof(PaymentEntryRemovedEvent), evt.ClientId, $"Entry removed from repository: {evt.ToJson()}");

            var removeEntryFromHistoryServiceCommand = new RemoveEntryFromHistoryServiceCommand
            {
                Id = evt.Id,
                ClientId = evt.ClientId,
                AssetId = evt.AssetId,
                Amount = evt.Amount,
                CashInId = evt.CashInId
            };

            commandSender.SendCommand(removeEntryFromHistoryServiceCommand, BoundedContexts.Payment);
        }
        
        [UsedImplicitly]
        public async Task Handle(CashInRemovedFromHistoryServiceEvent evt, ICommandSender commandSender)
        {
            _log.WriteInfo(nameof(CashInRemovedFromHistoryServiceEvent), evt.ClientId, $"Entry removed from OperationsHistory: {evt.ToJson()}");

            var removeEntryFromHistoryJobCommand = new RemoveEntryFromHistoryJobCommand
            {
                Id = evt.Id,
                ClientId = evt.ClientId,
                AssetId = evt.AssetId,
                Amount = evt.Amount,
                CashInId = evt.CashInId
            };

            commandSender.SendCommand(removeEntryFromHistoryJobCommand, BoundedContexts.Payment);
        }
        
        [UsedImplicitly]
        public async Task Handle(CashInRemovedFromHistoryJobEvent evt, ICommandSender commandSender)
        {
            _log.WriteInfo(nameof(CashInRemovedFromHistoryServiceEvent), evt.ClientId, $"Entry removed from OperationsHistory: {evt.ToJson()}");

            var resolvePaymentCommand = new ResolvePaymentCommand
            {
                Id = evt.Id,
                ClientId = evt.ClientId,
                AssetId = evt.AssetId,
                Amount = evt.Amount
            };

            commandSender.SendCommand(resolvePaymentCommand, BoundedContexts.Payment);
        }
        
        [UsedImplicitly]
        public async Task Handle(PaymentResolvedEvent evt, ICommandSender commandSender)
        {
            _log.WriteInfo(nameof(PaymentResolvedEvent), evt.ClientId, $"Payment resolved: {evt.ToJson()}");

            var processPaymentCommand = new ProcessPaymentCommand
            {
                Id = evt.Id,
                ClientId = evt.ClientId,
                AssetId = evt.AssetId,
                Amount = evt.Amount
            };

            commandSender.SendCommand(processPaymentCommand, BoundedContexts.Payment);
        }
    }
}
