using System;
using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Services.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using CalculateFunding.Services.Specs.Validators;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Services.Validators;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Models;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Services;

namespace CalculateFunding.Functions.Specs
{
    static public class IocConfig
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider Build(IConfigurationRoot config)
        {
            if (_serviceProvider == null)
                _serviceProvider = BuildServiceProvider(config);

            return _serviceProvider;
        }

        static public IServiceProvider BuildServiceProvider(IConfigurationRoot config)
        {
            var serviceProvider = new ServiceCollection();

            RegisterComponents(serviceProvider, config);

            return serviceProvider.BuildServiceProvider();
        }

        static public void RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            builder.AddSingleton<ISpecificationsRepository, SpecificationsRepository>();
            builder.AddSingleton<ISpecificationsService, SpecificationsService>();
            builder.AddSingleton<IValidator<PolicyCreateModel>, PolicyCreateModelValidator>();
            builder.AddSingleton<IValidator<PolicyEditModel>, PolicyEditModelValidator>();
            builder.AddSingleton<IValidator<CalculationCreateModel>, CalculationCreateModelValidator>();
            builder.AddSingleton<IValidator<SpecificationCreateModel>, SpecificationCreateModelValidator>();
            builder.AddSingleton<IValidator<CalculationEditModel>, CalculationEditModelValidator>();
            builder.AddSingleton<IValidator<SpecificationEditModel>, SpecificationEditModelValidator>();
            builder.AddSingleton<IValidator<AssignDefinitionRelationshipMessage>, AssignDefinitionRelationshipMessageValidator>();
            builder.AddSingleton<ISpecificationsSearchService, SpecificationsSearchService>();
            builder.AddSingleton<IResultsRepository, ResultsRepository>();

            builder.AddSingleton<IVersionRepository<SpecificationVersion>, VersionRepository<SpecificationVersion>>((ctx) =>
            {
                CosmosDbSettings specsVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", specsVersioningDbSettings);

                specsVersioningDbSettings.CollectionName = "specs";

                CosmosRepository resultsRepostory = new CosmosRepository(specsVersioningDbSettings);

                return new VersionRepository<SpecificationVersion>(resultsRepostory);
            });

            MapperConfiguration mappingConfig = new MapperConfiguration(c => c.AddProfile<SpecificationsMappingProfile>());

            builder.AddSingleton(mappingConfig.CreateMapper());

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddCosmosDb(config, "specs");
            }
            else
            {
                builder.AddCosmosDb(config);
            }

            builder.AddServiceBus(config);

            builder.AddSearch(config);

            builder.AddCaching(config);

            builder.AddResultsInterServiceClient(config);

            builder.AddPolicySettings(config);

            builder.AddFeatureToggling(config);

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Specs");
            builder.AddLogging("CalculateFunding.Functions.Specs");
            builder.AddTelemetry();
        }
    }
}
