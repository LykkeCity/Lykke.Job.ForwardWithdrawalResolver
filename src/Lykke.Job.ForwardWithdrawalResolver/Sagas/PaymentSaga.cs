using System.Threading.Tasks;
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
            _log.WriteInfo(nameof(PaymentSaga), evt, "Entry removed from repository");

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
            _log.WriteInfo(nameof(PaymentSaga), evt, "Entry removed from repository");

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
