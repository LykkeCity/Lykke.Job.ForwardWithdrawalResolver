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
        private readonly ICqrsEngine _cqrsEngine;
        private readonly TimeSpan _triggerSpan;
        private readonly IForwardWithdrawalRepository _repository;
        private readonly ILog _log;

        public PaymentDuePeriodicalHandler(
            ILog log,
            ICqrsEngine cqrsEngine,
            TimeSpan triggerSpan,
            IForwardWithdrawalRepository repository) :
            base(nameof(PaymentDuePeriodicalHandler), (int) TimeSpan.FromMinutes(10).TotalMilliseconds, log)
        {
            _log = log;
            _cqrsEngine = cqrsEngine;
            _triggerSpan = triggerSpan;
            _repository = repository;
        }

        public override async Task Execute()
        {
            foreach (var forwardWithdrawal in await _repository.GetAllAsync())
            {
                if (forwardWithdrawal.IsDue(_triggerSpan))
                {
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
        }
    }
}