using System;
using AutoMapper;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Functions.Specs.ServiceBus;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.Validators;
using CalculateFunding.Services.Validators;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.Specs.Startup))]

namespace CalculateFunding.Functions.Specs
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterComponents(builder.Services);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            return RegisterComponents(builder, config);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder, IConfigurationRoot config)
        {
            builder.AddSingleton<OnAddRelationshipEvent>();
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
            builder.AddSingleton<ICalculationsRepository, CalculationsRepository>();

            builder.AddSingleton<ICosmosRepository, CosmosRepository>();

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
            builder.AddCalcsInterServiceClient(config);

            builder.AddPolicySettings(config);

            builder.AddFeatureToggling(config);

            builder.AddApplicationInsights(config, "CalculateFunding.Functions.Specs");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Specs");
            builder.AddLogging("CalculateFunding.Functions.Specs");
            builder.AddTelemetry();

            return builder.BuildServiceProvider();
        }
    }
}
