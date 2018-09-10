using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.ForwardWithdrawalResolver.AzureRepositories;
using Lykke.Service.History.Contracts.Cqrs;
using Lykke.Service.History.Contracts.Cqrs.Commands;
using Lykke.Service.PostProcessing.Contracts.Cqrs.Events;

namespace Lykke.Job.ForwardWithdrawalResolver.Sagas
{
    [UsedImplicitly]
    public class CashoutsSaga
    {
        private readonly ILog _log;
        private readonly IForwardWithdrawalRepository _repository;

        public CashoutsSaga(IForwardWithdrawalRepository repository, ILogFactory logFactory)
        {
            _repository = repository;
            _log = logFactory.CreateLog(this);
        }

        public async Task Handle(CashOutProcessedEvent cashOutProcessedEvent, ICommandSender commandSender)
        {
            var record = await _repository.TryGetByCashoutIdAsync(cashOutProcessedEvent.WalletId.ToString(), cashOutProcessedEvent.OperationId.ToString());

            if (record != null)
            {
                if (!Guid.TryParse(record.CashInId, out var cashinId))
                {
                    _log.Warning($"Cannot parse data : {record.ToJson()}");
                    return;
                }

                commandSender.SendCommand(new CreateForwardCashinCommand
                {
                    AssetId = record.AssetId,
                    OperationId = cashinId,
                    Timestamp = record.DateTime,
                    Volume = Math.Abs(cashOutProcessedEvent.Volume),
                    WalletId = cashOutProcessedEvent.WalletId
                }, HistoryBoundedContext.Name);
            }
        }
    }
}