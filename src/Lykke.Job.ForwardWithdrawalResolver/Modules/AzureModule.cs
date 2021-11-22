using System;
using Autofac;
using AzureStorage.Tables;
using Lykke.Common.Log;
using Lykke.Job.ForwardWithdrawalResolver.AzureRepositories;
using Lykke.Job.ForwardWithdrawalResolver.Settings;
using Lykke.SettingsReader;

namespace Lykke.Job.ForwardWithdrawalResolver.Modules
{
    public class AzureModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;

        public AzureModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register<IForwardWithdrawalRepository>(c =>
                    new ForwardWithdrawalRepository(
                        AzureTableStorage<ForwardWithdrawalEntity>.Create(
                            _settings.ConnectionString(x =>
                                x.ForwardWithdrawalResolverJob.Db.ForwardWithdrawalsConnString),
                            "ForwardWithdrawal",
                            c.Resolve<ILogFactory>(),
                            TimeSpan.FromSeconds(60))))
                .SingleInstance();

            builder.Register<IBitCoinTransactionsRepository>(c =>
                new BitCoinTransactionsRepository(
                    AzureTableStorage<BitCoinTransactionEntity>.Create(
                        _settings.ConnectionString(x => x.ForwardWithdrawalResolverJob.Db.BitCoinQueueConnectionString),
                        "BitCoinTransactions", c.Resolve<ILogFactory>())));
        }
    }
}
