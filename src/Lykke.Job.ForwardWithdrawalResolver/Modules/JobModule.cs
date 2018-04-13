using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Job.ForwardWithdrawalResolver.AzureRepositories;
using Lykke.Job.ForwardWithdrawalResolver.Core.Services;
using Lykke.Job.ForwardWithdrawalResolver.Settings.JobSettings;
using Lykke.Job.ForwardWithdrawalResolver.Services;
using Lykke.SettingsReader;
using Lykke.Job.ForwardWithdrawalResolver.PeriodicalHandlers;
using Lykke.Job.ForwardWithdrawalResolver.Sagas;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Commands;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Events;
using Lykke.Job.ForwardWithdrawalResolver.Settings;
using Lykke.Job.OperationsCache.Client;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.OperationsHistory.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.ForwardWithdrawalResolver.Modules
{
    public class JobModule : Module
    {
        private readonly AppSettings _settings;
        private readonly IReloadingManager<DbSettings> _dbSettingsManager;
        private readonly ILog _log;
        private readonly IServiceCollection _services;

        public JobModule(AppSettings settings, IReloadingManager<DbSettings> dbSettingsManager, ILog log)
        {
            _settings = settings;
            _log = log;
            _dbSettingsManager = dbSettingsManager;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();
            
            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();

            
            builder.RegisterInstance<IPaymentResolver>(
                    new PaymentResolver(
                        _settings.ForwardWithdrawalResolverJob.AssetMappings,
                        _settings.ForwardWithdrawalResolverJob.ResolveAssetToItselfByDefault))
                .SingleInstance();
            
            
            builder.RegisterType<PaymentDuePeriodicalHandler>()
                .WithParameter("criticalSpan", TimeSpan.FromDays(_settings.ForwardWithdrawalResolverJob.CriticalDifferenceDays))
                .WithParameter("jobTriggerSpan", TimeSpan.FromMinutes(_settings.ForwardWithdrawalResolverJob.JobPeriodMinutes))
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();

            builder.Populate(_services);
        }
    }
}
