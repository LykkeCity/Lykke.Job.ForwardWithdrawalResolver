using Antares.Sdk;
using Autofac;
using JetBrains.Annotations;
using Lykke.Job.ForwardWithdrawalResolver.Settings;
using Lykke.SettingsReader;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.ForwardWithdrawalResolver
{
    public class Startup
    {
        private readonly LykkeSwaggerOptions _swaggerOptions = new LykkeSwaggerOptions
        {
            ApiTitle = "ForwardWithdrawalResolver API",
            ApiVersion = "v1"
        };

        private IReloadingManagerWithConfiguration<AppSettings> _settings;
        private LykkeServiceOptions<AppSettings> _lykkeOptions;

        public void ConfigureServices(IServiceCollection services)
        {
            (_lykkeOptions, _settings) = services.ConfigureServices<AppSettings>(options =>
            {
                options.SwaggerOptions = _swaggerOptions;

                options.Logs = logs =>
                {
                    logs.AzureTableName = "ForwardWithdrawalResolverLog";
                    logs.AzureTableConnectionStringResolver =
                        settings => settings.ForwardWithdrawalResolverJob.Db.LogsConnString;
                };
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseLykkeConfiguration(options => { options.SwaggerOptions = _swaggerOptions; });
        }

        [UsedImplicitly]
        public void ConfigureContainer(ContainerBuilder builder)
        {
            var configurationRoot = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            builder.ConfigureContainerBuilder(_lykkeOptions, configurationRoot, _settings);
        }
    }
}
