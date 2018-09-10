using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.ForwardWithdrawalResolver.AzureRepositories;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Commands;
using Lykke.Service.Assets.Client;

namespace Lykke.Job.ForwardWithdrawalResolver.PeriodicalHandlers
{
    [UsedImplicitly]
    public class PaymentDuePeriodicalHandler : TimerPeriod
    {
        private const string DifferenceTooBigErrorMessage =
            "Will not process: difference between DateTime and Timestamp values is too big.";

        private const string DaysToTriggerCouldNotBeResolvedErrorMessage =
            "Will not process: could not resolve forward asset for {0}";

        private readonly IAssetsService _assetsService;

        private readonly ICqrsEngine _cqrsEngine;
        private readonly TimeSpan _criticalSpan;
        private readonly ILog _log;
        private readonly IForwardWithdrawalRepository _repository;

        public PaymentDuePeriodicalHandler(
            ILogFactory logFactory,
            IAssetsService assetsService,
            ICqrsEngine cqrsEngine,
            TimeSpan criticalSpan,
            TimeSpan jobTriggerSpan,
            IForwardWithdrawalRepository repository) :
            base(TimeSpan.FromMilliseconds(jobTriggerSpan.TotalMilliseconds), logFactory)
        {
            _log = logFactory.CreateLog(this);
            _assetsService = assetsService;
            _cqrsEngine = cqrsEngine;
            _criticalSpan = criticalSpan;
            _repository = repository;
        }

        public override async Task Execute()
        {
            var assets = await _assetsService.AssetGetAllAsync();

            foreach (var forwardWithdrawal in await _repository.GetAllAsync())
                try
                {
                    var daysToTrigger = assets.FirstOrDefault(x => x.Id == forwardWithdrawal.AssetId)
                        ?.ForwardFrozenDays;

                    if (!daysToTrigger.HasValue)
                        throw new InvalidOperationException(string.Format(DaysToTriggerCouldNotBeResolvedErrorMessage,
                            forwardWithdrawal.AssetId));

                    if (forwardWithdrawal.IsDue(TimeSpan.FromDays(daysToTrigger.Value)))
                    {
                        if (forwardWithdrawal.DateTimeTimestampDifferenceTooBig(_criticalSpan))
                            throw new InvalidOperationException(DifferenceTooBigErrorMessage);

                        _cqrsEngine.SendCommand(
                            new RemoveEntryCommand
                            {
                                ClientId = forwardWithdrawal.ClientId,
                                Id = forwardWithdrawal.Id
                            },
                            BoundedContext.ForwardWithdrawal,
                            BoundedContext.ForwardWithdrawal);
                    }
                }
                catch (Exception e)
                {
                    _log.Error(e, forwardWithdrawal.ToJson());
                }
        }
    }
}