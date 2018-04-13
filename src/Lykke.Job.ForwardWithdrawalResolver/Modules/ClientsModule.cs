using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Job.ForwardWithdrawalResolver.Settings;
using Lykke.Job.ForwardWithdrawalResolver.Settings.JobSettings;
using Lykke.Job.OperationsCache.Client;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.OperationsHistory.Client;
using Lykke.Service.Assets.Client;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.ForwardWithdrawalResolver.Modules
{
    public class ClientsModule : Module
    {
        private readonly AppSettings _settings;
        private readonly IReloadingManager<DbSettings> _dbSettingsManager;
        private readonly ILog _log;
        private readonly IServiceCollection _services;

        public ClientsModule(AppSettings settings, IReloadingManager<DbSettings> dbSettingsManager, ILog log)
        {
            _settings = settings;
            _log = log;
            _dbSettingsManager = dbSettingsManager;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterOperationsHistoryClient(_settings.OperationsHistoryServiceClient, _log);
            builder.RegisterOperationsCacheClient(_settings.OperationsCacheJobClient, _log);
            
            builder
                .RegisterInstance(
                    new ExchangeOperationsServiceClient(_settings.ExchangeOperationsServiceClient.ServiceUrl))
                .As<IExchangeOperationsServiceClient>()
                .SingleInstance();
            
            _services.RegisterAssetsClient(AssetServiceSettings.Create(new Uri(_settings.AssetsServiceClient.ServiceUrl),
                TimeSpan.FromMinutes(1)));
            
            builder.Populate(_services);
        }
    }
}