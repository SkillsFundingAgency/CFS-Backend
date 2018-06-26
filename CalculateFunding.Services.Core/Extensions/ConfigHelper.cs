using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class ConfigHelper
    {
        public static IConfigurationRoot AddConfig()
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables();

            if(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                // Add user secrets for CalculateFunding.Functions.LocalDebugProxy
                configBuilder.AddUserSecrets("df0d69d5-a6db-4598-909f-262fc39cb8c8");
            }

            return configBuilder.Build();
        }
    }
}
