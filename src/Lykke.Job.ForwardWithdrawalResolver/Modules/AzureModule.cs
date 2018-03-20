using Autofac;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Job.ForwardWithdrawalResolver.AzureRepositories;
using Lykke.Job.ForwardWithdrawalResolver.Settings;
using Lykke.Job.ForwardWithdrawalResolver.Settings.JobSettings;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.ForwardWithdrawalResolver.Modules
{
    public class AzureModule : Module
    {
        private readonly AppSettings _settings;
        private readonly IReloadingManager<DbSettings> _dbSettingsManager;
        private readonly ILog _log;
        private readonly IServiceCollection _services;

        public AzureModule(AppSettings settings, IReloadingManager<DbSettings> dbSettingsManager, ILog log)
        {
            _settings = settings;
            _log = log;
            _dbSettingsManager = dbSettingsManager;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance<IForwardWithdrawalRepository>(
                    new ForwardWithdrawalRepository(
                        AzureTableStorage<ForwardWithdrawalEntity>.Create(
                            _dbSettingsManager.ConnectionString(x => x.ForwardWithdrawalsConnString),
                            "ForwardWithdrawal",
                            _log)))
                .SingleInstance();
        }
    }
}