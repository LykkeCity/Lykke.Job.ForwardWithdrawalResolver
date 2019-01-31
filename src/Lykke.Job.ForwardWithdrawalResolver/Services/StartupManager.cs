using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Sdk;

namespace Lykke.Job.ForwardWithdrawalResolver.Services
{
    [UsedImplicitly]
    public class StartupManager : IStartupManager
    {
        private readonly ICqrsEngine _cqrsEngine;

        public StartupManager(ICqrsEngine cqrsEngine)
        {
            _cqrsEngine = cqrsEngine;
        }

        public Task StartAsync()
        {
            _cqrsEngine.StartSubscribers();

            return Task.CompletedTask;
        }
    }
}
