using System;
using CalculateFunding.Models;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Scenarios;
using CalculateFunding.Services.Scenarios.Interfaces;
using CalculateFunding.Services.Scenarios.Validators;
using FluentValidation;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Scenarios
{
    static public class IocConfig
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider Build()
        {
            if (_serviceProvider == null)
                _serviceProvider = BuildServiceProvider();

            return _serviceProvider;
        }

        static public IServiceProvider BuildServiceProvider()
        {
            var serviceProvider = new ServiceCollection();

            RegisterComponents(serviceProvider);

            return serviceProvider.BuildServiceProvider();
        }

        static public IServiceProvider BuildServiceProvider(Message message)
        {
            var serviceProvider = new ServiceCollection();

            serviceProvider.AddUserProviderFromMessage(message);

            RegisterComponents(serviceProvider);

            return serviceProvider.BuildServiceProvider();
        }

        static public void RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            builder.AddSingleton<IScenariosRepository, ScenariosRepository>();
            builder.AddSingleton<IScenariosService, ScenariosService>();
            builder.AddSingleton<IScenariosSearchService, ScenariosSearchService>();
            builder
                .AddSingleton<IValidator<CreateNewTestScenarioVersion>, CreateNewTestScenarioVersionValidator>();
            builder
                .AddSingleton<ISpecificationsRepository, SpecificationsRepository>();

            builder
               .AddSingleton<IBuildProjectRepository, BuildProjectRepository>();

            builder.AddCalcsInterServiceClient(config);
            builder.AddSpecificationsInterServiceClient(config);

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddCosmosDb(config, "tests");
            }
            else
            {
                builder.AddCosmosDb(config);
            }

            builder.AddSearch(config);

            builder.AddServiceBus(config);

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config);

            builder.AddLogging("CalculateFunding.Functions.Scenarios");

            builder.AddTelemetry();
        }
    }
}
