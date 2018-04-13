using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.ForwardWithdrawalResolver.AzureRepositories;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Commands;

namespace Lykke.Job.ForwardWithdrawalResolver.PeriodicalHandlers
{
    [UsedImplicitly]
    public class PaymentDuePeriodicalHandler : TimerPeriod
    {
        private const string DifferenceTooBigErrorMessage =
            "Will not process, because difference between DateTime and Timestamp values is too big.";
        
        private readonly ICqrsEngine _cqrsEngine;
        private readonly TimeSpan _triggerSpan;
        private readonly TimeSpan _criticalSpan;
        private readonly IForwardWithdrawalRepository _repository;
        private readonly ILog _log;

        public PaymentDuePeriodicalHandler(
            ILog log,
            ICqrsEngine cqrsEngine,
            TimeSpan triggerSpan,
            TimeSpan criticalSpan,
            TimeSpan jobTriggerSpan,
            IForwardWithdrawalRepository repository) :
            base(nameof(PaymentDuePeriodicalHandler), (int) jobTriggerSpan.TotalMilliseconds, log)
        {
            _log = log;
            _cqrsEngine = cqrsEngine;
            _triggerSpan = triggerSpan;
            _criticalSpan = criticalSpan;
            _repository = repository;
        }

        public override async Task Execute()
        {
            foreach (var forwardWithdrawal in await _repository.GetAllAsync())
            {
                if (forwardWithdrawal.IsDue(_triggerSpan))
                {
                    try
                    {
                        if(forwardWithdrawal.DateTimeTimestampDifferenceTooBig(_criticalSpan))
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
                    catch (Exception e)
                    {
                        _log.WriteError(nameof(PaymentDuePeriodicalHandler), forwardWithdrawal.ToJson(), e);
                    }
                }
            }
        }
    }
}