using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.ForwardWithdrawalResolver.AzureRepositories;
using Lykke.Service.Assets.Client;
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
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;

        public CashoutsSaga(IForwardWithdrawalRepository repository, ILogFactory logFactory, IAssetsServiceWithCache assetsServiceWithCache)
        {
            _repository = repository;
            _assetsServiceWithCache = assetsServiceWithCache;
            _log = logFactory.CreateLog(this);
        }

        public async Task<CommandHandlingResult> Handle(CashOutProcessedEvent cashOutProcessedEvent, ICommandSender commandSender)
        {
            var record = await _repository.TryGetByCashoutIdAsync(cashOutProcessedEvent.WalletId.ToString(), cashOutProcessedEvent.OperationId.ToString());

            if (record != null)
            {
                if (!Guid.TryParse(record.CashInId, out var cashinId))
                {
                    _log.Warning($"Cannot parse data : {record.ToJson()}");
                    return CommandHandlingResult.Ok();
                }

                var asset = await _assetsServiceWithCache.TryGetAssetAsync(record.AssetId);
                var forwardAsset = await _assetsServiceWithCache.TryGetAssetAsync(asset.ForwardBaseAsset);

                var settlementDate = record.DateTime.AddDays(asset.ForwardFrozenDays);

                var command = new CreateForwardCashinCommand
                {
                    AssetId = forwardAsset.Id,
                    OperationId = cashinId,
                    Timestamp = settlementDate,
                    Volume = Math.Abs(cashOutProcessedEvent.Volume).TruncateDecimalPlaces(forwardAsset.Accuracy),
                    WalletId = cashOutProcessedEvent.WalletId
                };

                commandSender.SendCommand(command, HistoryBoundedContext.Name);

                _log.Info("CreateForwardCashinCommand has been sent.", command);
                return CommandHandlingResult.Ok();
            }

            _log.Warning("No forward withdrawal record found.", context: new { Id = cashOutProcessedEvent.OperationId.ToString() });
            return CommandHandlingResult.Fail(TimeSpan.FromSeconds(10));
        }
    }
}
