using CalculateFunding.Functions.Common.Interfaces.Logging;
using CalculateFunding.Functions.Common.Logging;
using CalculateFunding.Functions.Common.Options;
using CalculateFunding.Repositories.Common.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;
using System.Text;

namespace CalculateFunding.Functions.Common.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCosmosDb(this IServiceCollection builder, IConfigurationRoot config)
        {
            CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

            config.Bind("CosmosDbSettings", cosmosDbSettings);

            builder.AddSingleton<CosmosDbSettings>(cosmosDbSettings);

            builder
                .AddScoped<CosmosRepository>();

            return builder;
        }

        public static IServiceCollection AddLogging(this IServiceCollection builder, IConfigurationRoot config)
        {
            builder
                .AddScoped<ILoggingService, ApplicationInsightsService>();

            ApplicationInsightsOptions appInsightsOptions = new ApplicationInsightsOptions();

            config.Bind("ApplicationInsightsOptions", appInsightsOptions);

            builder.AddSingleton<ApplicationInsightsOptions>(appInsightsOptions);

            return builder;
        }
    }
}
