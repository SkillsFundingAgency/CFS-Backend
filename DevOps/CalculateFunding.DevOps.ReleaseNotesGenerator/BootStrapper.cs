using CalculateFunding.DevOps.ReleaseNotesGenerator.Generators;
using CalculateFunding.DevOps.ReleaseNotesGenerator.Options;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CalculateFunding.DevOps.ReleaseNotesGenerator
{
    internal class BootStrapper
    {
        private const string ServiceName = "CalculateFunding.DevOps.ReleaseNotesGenerator";

        private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
            .AddJsonFile("appSettings.json")
            .Build();

        public static IServiceProvider BuildServiceProvider()
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddApplicationInsightsTelemetryClient(
                Configuration,
                ServiceName);
            serviceCollection.AddLogging(serviceName: ServiceName);

            serviceCollection.Configure<ReleaseDefinitionOptions>(Configuration.GetSection(typeof(ReleaseDefinitionOptions).Name));

            serviceCollection.AddTransient<INotesGenerator, NotesGenerator>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
