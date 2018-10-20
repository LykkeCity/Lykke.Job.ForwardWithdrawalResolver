using System;
using Autofac;
using Lykke.Job.ForwardWithdrawalResolver.Settings;
using Lykke.Job.OperationsCache.Client;
using Lykke.Logs;
using Lykke.Service.Assets.Client;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.SettingsReader;

namespace Lykke.Job.ForwardWithdrawalResolver.Modules
{
    public class ClientsModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;

        public ClientsModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var emptyLog = EmptyLogFactory.Instance.CreateLog(this);

            builder.RegisterInstance(
                    new OperationsCacheClient(_settings.CurrentValue.OperationsCacheJobClient.ServiceUrl, emptyLog))
                .As<IOperationsCacheClient>();

            builder
                .RegisterInstance(
                    new ExchangeOperationsServiceClient(_settings.CurrentValue.ExchangeOperationsServiceClient
                        .ServiceUrl))
                .As<IExchangeOperationsServiceClient>()
                .SingleInstance();

            builder.RegisterAssetsClient(AssetServiceSettings.Create(
                new Uri(_settings.CurrentValue.AssetsServiceClient.ServiceUrl),
                TimeSpan.FromMinutes(60)));
        }
    }
}