using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.ForwardWithdrawalResolver.Core.Services;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Commands;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Events;
using Lykke.Service.History.Contracts.Cqrs;
using Lykke.Service.History.Contracts.Cqrs.Commands;

namespace Lykke.Job.ForwardWithdrawalResolver.Sagas
{
    [UsedImplicitly]
    public class PaymentSaga
    {
        private readonly ILog _log;
        private readonly IPaymentResolver _paymentResolver;

        public PaymentSaga(
            ILogFactory logFactory,
            IPaymentResolver paymentResolver)
        {
            _log = logFactory.CreateLog(this);
            _paymentResolver = paymentResolver;
        }

        [UsedImplicitly]
        public async Task Handle(PaymentEntryRemovedEvent evt, ICommandSender commandSender)
        {
            var removeEntryFromHistoryServiceCommand = new RemoveEntryFromHistoryServiceCommand
            {
                Id = evt.Id,
                ClientId = evt.ClientId,
                AssetId = evt.AssetId,
                Amount = evt.Amount,
                CashInId = evt.CashInId
            };

            commandSender.SendCommand(removeEntryFromHistoryServiceCommand, BoundedContext.ForwardWithdrawal);
        }

        [UsedImplicitly]
        public async Task Handle(CashInRemovedFromHistoryServiceEvent evt, ICommandSender commandSender)
        {
            var removeEntryFromHistoryJobCommand = new RemoveEntryFromHistoryJobCommand
            {
                Id = evt.Id,
                ClientId = evt.ClientId,
                AssetId = evt.AssetId,
                Amount = evt.Amount,
                CashInId = evt.CashInId
            };

            commandSender.SendCommand(removeEntryFromHistoryJobCommand, BoundedContext.ForwardWithdrawal);
        }

        [UsedImplicitly]
        public async Task Handle(CashInRemovedFromHistoryJobEvent evt, ICommandSender commandSender)
        {
            var assetToPayId = await _paymentResolver.Resolve(evt.AssetId);

            var processPaymentCommand = new ProcessPaymentCommand
            {
                Id = evt.Id,
                ClientId = evt.ClientId,
                AssetId = assetToPayId,
                Amount = evt.Amount,
                NewCashinId = Guid.NewGuid(),
                CashinId = evt.CashInId
            };

            commandSender.SendCommand(processPaymentCommand, BoundedContext.ForwardWithdrawal);
        }

        [UsedImplicitly]
        public async Task Handle(CashInProcesedEvent evt, ICommandSender commandSender)
        {
            if (!Guid.TryParse(evt.OperationId, out var operationId) || !Guid.TryParse(evt.ClientId, out var clientId))
            {
                _log.Warning("Cannot parse data", context: evt);
                return;
            }

            var removeCashinCommand = new DeleteForwardCashinCommand
            {
                OperationId = operationId,
                WalletId = clientId
            };

            commandSender.SendCommand(removeCashinCommand, HistoryBoundedContext.Name);
        }
    }
}