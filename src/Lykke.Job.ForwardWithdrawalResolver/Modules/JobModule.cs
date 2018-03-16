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
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using Lykke.Service.ExchangeOperations.Client;
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

            RegisterServices(builder);
            
            RegisterAzureRepositories(builder);

            RegisterServiceClients(builder);

            RegisterCqrs(builder);

            RegisterPeriodicalHandlers(builder);

            builder.Populate(_services);
        }

        private void RegisterServices(ContainerBuilder builder)
        {
            builder.RegisterInstance<IPaymentResolver>(
                    new PaymentResolver(
                        _settings.ForwardWithdrawalResolverJob.AssetMappings,
                        _settings.ForwardWithdrawalResolverJob.ResolveAssetToItselfByDefault))
                .SingleInstance();
        }

        private void RegisterAzureRepositories(ContainerBuilder builder)
        {
            builder.RegisterInstance<IForwardWithdrawalRepository>(
                new ForwardWithdrawalRepository(
                    AzureTableStorage<ForwardWithdrawalEntity>.Create(
                    _dbSettingsManager.ConnectionString(x => x.ForwardWithdrawalsConnString),
                    "ForwardWithdrawal",
                    _log)))
                .SingleInstance();
        }

        private void RegisterPeriodicalHandlers(ContainerBuilder builder)
        {
            builder.RegisterType<PaymentDuePeriodicalHandler>()
                .WithParameter(TypedParameter.From(TimeSpan.FromDays(_settings.ForwardWithdrawalResolverJob.DaysToTrigger)))
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();
        }

        private void RegisterServiceClients(ContainerBuilder builder)
        {
            builder
                .RegisterInstance(
                    new ExchangeOperationsServiceClient(_settings.ExchangeOperationsServiceClient.ServiceUrl))
                .As<IExchangeOperationsServiceClient>()
                .SingleInstance();
        }
        
        private void RegisterCqrs(ContainerBuilder builder)
        {
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory { Uri = _settings.ForwardWithdrawalResolverJob.Rabbit.ConnectionString };
            
            var messagingEngine = new MessagingEngine(_log,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {"RabbitMq", new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName, rabbitMqSettings.Password, "None", "RabbitMq")}
                }),
                new RabbitMqTransportFactory());
            
            builder.RegisterType<PaymentSaga>()
                .SingleInstance();
            
            builder.RegisterType<CommandsHandler>()
                .WithParameter(TypedParameter.From(_settings.ForwardWithdrawalResolverJob.HotWallet))
                .SingleInstance();

            builder.Register(ctx =>
            {
                const string defaultPipeline = "commands";
                const string defaultRoute = "self";

                return new CqrsEngine(
                    _log,
                    ctx.Resolve<IDependencyResolver>(),
                    messagingEngine,
                    new DefaultEndpointProvider(),
                    true,

                    Register.DefaultEndpointResolver(
                        new RabbitMqConventionEndpointResolver("RabbitMq", "messagepack", "lykke")),

                    Register.BoundedContext(BoundedContexts.Payment)
                        .ListeningCommands(
                            typeof(ProcessPaymentCommand),
                            typeof(RemoveEntryCommand),
                            typeof(ResolvePaymentCommand))
                        .On(defaultRoute)
                        .PublishingEvents(
                            typeof(PaymentEntryRemovedEvent),
                            typeof(PaymentResolvedEvent))
                        .With(defaultPipeline)
                        .WithCommandsHandler(typeof(CommandsHandler)),

                    Register.Saga<PaymentSaga>("payment-saga")
                        .ListeningEvents(
                            typeof(PaymentEntryRemovedEvent),
                            typeof(PaymentResolvedEvent))
                        .From(BoundedContexts.Payment).On(defaultRoute)
                        .PublishingCommands(
                            typeof(ProcessPaymentCommand),
                            typeof(RemoveEntryCommand),
                            typeof(ResolvePaymentCommand))
                        .To(BoundedContexts.Payment).With(defaultPipeline),
                        
                    Register.DefaultRouting
                        .PublishingCommands(typeof(RemoveEntryCommand))
                        .To(BoundedContexts.Payment)
                        .With(defaultPipeline)
                );
            }).As<ICqrsEngine>().SingleInstance();

        }
    }
}
