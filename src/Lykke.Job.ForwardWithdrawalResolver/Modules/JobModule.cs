using System;
using Antares.Sdk.Services;
using Autofac;
using Lykke.Job.ForwardWithdrawalResolver.Core.Services;
using Lykke.Job.ForwardWithdrawalResolver.PeriodicalHandlers;
using Lykke.Job.ForwardWithdrawalResolver.Services;
using Lykke.Job.ForwardWithdrawalResolver.Settings;
using Lykke.Job.ForwardWithdrawalResolver.Settings.JobSettings;
using Lykke.SettingsReader;

namespace Lykke.Job.ForwardWithdrawalResolver.Modules
{
    public class JobModule : Module
    {
        private readonly ForwardWithdrawalResolverSettings _settings;

        public JobModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings.CurrentValue.ForwardWithdrawalResolverJob;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder.RegisterInstance<IPaymentResolver>(
                    new PaymentResolver(
                        _settings.AssetMappings,
                        _settings.ResolveAssetToItselfByDefault))
                .SingleInstance();

            builder.RegisterType<PaymentDuePeriodicalHandler>()
                .WithParameter("criticalSpan", TimeSpan.FromDays(_settings.CriticalDifferenceDays))
                .WithParameter("jobTriggerSpan", TimeSpan.FromMinutes(_settings.JobPeriodMinutes))
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();
        }
    }
}
