using System.Collections.Generic;
using Autofac;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Job.ForwardWithdrawalResolver.Sagas;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Commands;
using Lykke.Job.ForwardWithdrawalResolver.Sagas.Events;
using Lykke.Job.ForwardWithdrawalResolver.Settings;
using Lykke.Job.ForwardWithdrawalResolver.Settings.JobSettings;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Serialization;
using Lykke.Service.History.Contracts.Cqrs;
using Lykke.Service.History.Contracts.Cqrs.Commands;
using Lykke.Service.PostProcessing.Contracts.Cqrs;
using Lykke.Service.PostProcessing.Contracts.Cqrs.Events;
using Lykke.SettingsReader;
using RabbitMQ.Client;

namespace Lykke.Job.ForwardWithdrawalResolver.Modules
{
    public class CqrsModule : Module
    {
        private readonly ForwardWithdrawalResolverSettings _settings;

        public CqrsModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings.CurrentValue.ForwardWithdrawalResolverJob;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>()
                .SingleInstance();

            var rabbitMqSettings = new ConnectionFactory
            {
                Uri = _settings.Cqrs.RabbitConnString
            };
            var rabbitMqEndpoint = rabbitMqSettings.Endpoint.ToString();

            builder.RegisterType<PaymentSaga>()
                .SingleInstance();

            builder.RegisterType<CashoutsSaga>()
                .SingleInstance();

            builder.RegisterType<CommandsHandler>()
                .WithParameter(TypedParameter.From(_settings.HotWallet))
                .SingleInstance();

            builder.Register(ctx =>
                {
                    var logFactory = ctx.Resolve<ILogFactory>();
                    var messagingEngine = new MessagingEngine(
                        logFactory,
                        new TransportResolver(
                            new Dictionary<string, TransportInfo>
                            {
                                {
                                    "RabbitMq",
                                    new TransportInfo(
                                        rabbitMqEndpoint,
                                        rabbitMqSettings.UserName,
                                        rabbitMqSettings.Password, "None", "RabbitMq")
                                }
                            }),
                        new RabbitMqTransportFactory(logFactory));
                    return CreateEngine(ctx, messagingEngine, logFactory);
                })
                .As<ICqrsEngine>()
                .AutoActivate()
                .SingleInstance();
        }

        private CqrsEngine CreateEngine(
            IComponentContext ctx,
            IMessagingEngine messagingEngine,
            ILogFactory logFactory)
        {
            const string defaultPipeline = "commands";
            const string defaultRoute = "self";

            var sagasMessagePackEndpointResolver = new RabbitMqConventionEndpointResolver(
                "RabbitMq",
                SerializationFormat.MessagePack,
                environment: "lykke");

            var sagasProtobufEndpointResolver = new RabbitMqConventionEndpointResolver(
                "RabbitMq",
                SerializationFormat.ProtoBuf,
                environment: "lykke");

            return new CqrsEngine(
                logFactory,
                ctx.Resolve<IDependencyResolver>(),
                messagingEngine,
                new DefaultEndpointProvider(),
                true,
                Register.DefaultEndpointResolver(sagasMessagePackEndpointResolver),
                Register.BoundedContext(BoundedContext.ForwardWithdrawal)
                    .ListeningCommands(
                        typeof(ProcessPaymentCommand),
                        typeof(RemoveEntryCommand),
                        typeof(RemoveEntryFromHistoryJobCommand),
                        typeof(RemoveEntryFromHistoryServiceCommand))
                    .On(defaultRoute)
                    .PublishingEvents(
                        typeof(PaymentEntryRemovedEvent),
                        typeof(CashInRemovedFromHistoryJobEvent),
                        typeof(CashInRemovedFromHistoryServiceEvent),
                        typeof(PaymentEntryRemovedEvent))
                    .With(defaultPipeline)
                    .WithCommandsHandler(typeof(CommandsHandler)),
                Register.Saga<PaymentSaga>("payment-saga")
                    .ListeningEvents(
                        typeof(PaymentEntryRemovedEvent),
                        typeof(CashInRemovedFromHistoryJobEvent),
                        typeof(CashInRemovedFromHistoryServiceEvent),
                        typeof(CashInProcesedEvent))
                    .From(BoundedContext.ForwardWithdrawal).On(defaultRoute)
                    .PublishingCommands(
                        typeof(ProcessPaymentCommand),
                        typeof(RemoveEntryCommand),
                        typeof(RemoveEntryFromHistoryJobCommand),
                        typeof(RemoveEntryFromHistoryServiceCommand))
                    .To(BoundedContext.ForwardWithdrawal).With(defaultPipeline)
                    .PublishingCommands(
                        typeof(DeleteForwardCashinCommand))
                    .To(HistoryBoundedContext.Name)
                    .With(defaultPipeline)
                    .WithEndpointResolver(sagasProtobufEndpointResolver),
                Register.Saga<CashoutsSaga>("cashouts-saga")
                    .ListeningEvents(typeof(CashOutProcessedEvent))
                    .From(PostProcessingBoundedContext.Name)
                    .On(defaultRoute)
                    .WithEndpointResolver(sagasProtobufEndpointResolver)
                    .PublishingCommands(
                        typeof(CreateForwardCashinCommand))
                    .To(HistoryBoundedContext.Name)
                    .With(defaultPipeline)
                    .WithEndpointResolver(sagasProtobufEndpointResolver),
                Register.DefaultRouting
                    .PublishingCommands(typeof(RemoveEntryCommand))
                    .To(BoundedContext.ForwardWithdrawal)
                    .With(defaultPipeline));
        }
    }
}