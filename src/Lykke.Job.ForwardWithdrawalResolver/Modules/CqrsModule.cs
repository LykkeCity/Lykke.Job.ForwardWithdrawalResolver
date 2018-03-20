using System.Collections.Generic;
using Autofac;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Job.ForwardWithdrawalResolver.Sagas;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Commands;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Events;
using Lykke.Job.ForwardWithdrawalResolver.Settings;
using Lykke.Job.ForwardWithdrawalResolver.Settings.JobSettings;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.ForwardWithdrawalResolver.Modules
{
    public class CqrsModule : Module
    {
        private readonly AppSettings _settings;
        private readonly IReloadingManager<DbSettings> _dbSettingsManager;
        private readonly ILog _log;
        private readonly IServiceCollection _services;

        public CqrsModule(AppSettings settings, IReloadingManager<DbSettings> dbSettingsManager, ILog log)
        {
            _settings = settings;
            _log = log;
            _dbSettingsManager = dbSettingsManager;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
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
                        new RabbitMqConventionEndpointResolver(
                            "RabbitMq",
                            "messagepack",
                            "forwardwithdrawalresolver",
                            "lykke")),

                    Register.BoundedContext(BoundedContexts.Payment)
                        .ListeningCommands(
                            typeof(ProcessPaymentCommand),
                            typeof(RemoveEntryCommand),
                            typeof(RemoveEntryFromHistoryJobCommand),
                            typeof(RemoveEntryFromHistoryServiceCommand),
                            typeof(ResolvePaymentCommand))
                        .On(defaultRoute)
                        .PublishingEvents(
                            typeof(PaymentEntryRemovedEvent),
                            typeof(PaymentResolvedEvent),
                            typeof(CashInRemovedFromHistoryJobEvent),
                            typeof(CashInRemovedFromHistoryServiceEvent))
                        .With(defaultPipeline)
                        .WithCommandsHandler(typeof(CommandsHandler)),

                    Register.Saga<PaymentSaga>("payment-saga")
                        .ListeningEvents(
                            typeof(PaymentEntryRemovedEvent),
                            typeof(PaymentResolvedEvent),
                            typeof(CashInRemovedFromHistoryJobEvent),
                            typeof(CashInRemovedFromHistoryServiceEvent))
                        .From(BoundedContexts.Payment).On(defaultRoute)
                        .PublishingCommands(
                            typeof(ProcessPaymentCommand),
                            typeof(RemoveEntryCommand),
                            typeof(ResolvePaymentCommand),
                            typeof(RemoveEntryFromHistoryJobCommand),
                            typeof(RemoveEntryFromHistoryServiceCommand))
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