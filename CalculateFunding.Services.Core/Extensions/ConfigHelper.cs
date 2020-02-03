using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using CalculateFunding.Services.Core.Caching.FileSystem;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class ConfigHelper
    {
        private static readonly string appConfigConnectionString = Environment.GetEnvironmentVariable("AzureConfiguration:ConnectionString");

        public static IConfigurationRoot AddConfig()
        {
            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .AddAppConfiguration();

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                // Add user secrets for CalculateFunding.Functions.LocalDebugProxy
                configBuilder.AddJsonFile("appsettings.development.json", true);
                configBuilder.AddUserSecrets("df0d69d5-a6db-4598-909f-262fc39cb8c8");
            }

            return configBuilder.Build();
        }

        public static void LoadConfiguration(IConfigurationBuilder configBuilder)
        {
            configBuilder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .AddAppConfiguration();

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                // Add user secrets for CalculateFunding.Functions.LocalDebugProxy
                configBuilder.AddJsonFile("appsettings.development.json", true);
                configBuilder.AddUserSecrets("df0d69d5-a6db-4598-909f-262fc39cb8c8");
            }
        }

        private static IConfigurationBuilder AddAppConfiguration(this IConfigurationBuilder configBuilder)
        {
            if (!string.IsNullOrEmpty(appConfigConnectionString))
            {
                configBuilder.AddAzureAppConfiguration(options =>
                {
                    options.Connect(appConfigConnectionString)
                        .UseFeatureFlags()
                        .ConfigureRefresh(refresh =>
                        {
                            refresh.Register("publishingengineoptions:GetCalculationResultsConcurrencyCount", refreshAll: true)
                                .Register($"{FileSystemCacheSettings.SectionName}:{nameof(IFileSystemCacheSettings.Prefix)}", true)
                                .SetCacheExpiration(TimeSpan.FromSeconds(1));
                        });
                });
            }

            return configBuilder;
        }
    }
}
