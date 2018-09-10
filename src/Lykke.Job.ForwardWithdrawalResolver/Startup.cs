using System;
using Lykke.Job.ForwardWithdrawalResolver.Settings;
using Lykke.Sdk;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return services.BuildServiceProvider<AppSettings>(options =>
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

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            app.UseLykkeConfiguration(options => { options.SwaggerOptions = _swaggerOptions; });
        }
    }
}