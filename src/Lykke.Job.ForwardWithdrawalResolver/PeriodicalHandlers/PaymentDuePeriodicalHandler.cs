using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
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

        private readonly ICqrsEngine _cqrsEngine;
        private readonly TimeSpan _criticalSpan;
        private readonly IForwardWithdrawalRepository _repository;
        private readonly IAssetsService _assetsService; 
        private readonly ILog _log;

        public PaymentDuePeriodicalHandler(
            ILog log,
            IAssetsService assetsService,
            ICqrsEngine cqrsEngine,
            TimeSpan criticalSpan,
            TimeSpan jobTriggerSpan,
            IForwardWithdrawalRepository repository) :
            base(nameof(PaymentDuePeriodicalHandler), (int) jobTriggerSpan.TotalMilliseconds, log)
        {
            _log = log;
            _assetsService = assetsService;
            _cqrsEngine = cqrsEngine;
            _criticalSpan = criticalSpan;
            _repository = repository;
        }

        public override async Task Execute()
        {
            var assets = await _assetsService.AssetGetAllAsync();
            
            foreach (var forwardWithdrawal in await _repository.GetAllAsync())
            {
                try
                {
                    var daysToTrigger = assets.FirstOrDefault(x => x.Id == forwardWithdrawal.AssetId)
                        ?.ForwardFrozenDays;
                    
                    if(!daysToTrigger.HasValue)
                        throw new InvalidOperationException(string.Format(DaysToTriggerCouldNotBeResolvedErrorMessage, forwardWithdrawal.AssetId));
                    
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
                            BoundedContexts.Payment,
                            BoundedContexts.Payment);
                    }
                }
                catch (Exception e)
                {
                    _log.WriteError(nameof(PaymentDuePeriodicalHandler), forwardWithdrawal.ToJson(), e);
                }
            }
        }
    }
}