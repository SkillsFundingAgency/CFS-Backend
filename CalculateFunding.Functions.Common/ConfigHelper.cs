using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CalculateFunding.Functions.Common
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

            return configBuilder.Build();
        }
    }
}
