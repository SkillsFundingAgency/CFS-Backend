using System;
using AutoMapper;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Schema10;
using CalculateFunding.Functions.Specs.ServiceBus;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.AzureStorage;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.MappingProfiles;
using CalculateFunding.Services.Specs.Validators;
using CalculateFunding.Services.Validators;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using Serilog;

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
            builder.AddSingleton<IQueueCreateSpecificationJobActions, QueueCreateSpecificationJobAction>();
            builder.AddSingleton<IValidator<SpecificationCreateModel>, SpecificationCreateModelValidator>();
            builder.AddSingleton<IValidator<SpecificationEditModel>, SpecificationEditModelValidator>();
            builder.AddSingleton<IValidator<AssignDefinitionRelationshipMessage>, AssignDefinitionRelationshipMessageValidator>();
            builder.AddSingleton<ISpecificationsSearchService, SpecificationsSearchService>();
            builder.AddSingleton<IResultsRepository, ResultsRepository>();
            builder.AddSingleton<ICalculationsRepository, CalculationsRepository>();

            builder.AddSingleton<ICosmosRepository, CosmosRepository>();

            builder.AddSingleton<ITemplateMetadataResolver>((ctx) =>
            {
                TemplateMetadataResolver resolver = new TemplateMetadataResolver();

                TemplateMetadataGenerator schema10Generator = new TemplateMetadataGenerator(ctx.GetService<ILogger>());

                resolver.Register("1.0", schema10Generator);

                return resolver;
            });

            builder
                .AddSingleton<IBlobClient, BlobClient>((ctx) =>
                {
                    AzureStorageSettings storageSettings = new AzureStorageSettings();

                    config.Bind("AzureStorageSettings", storageSettings);

                    storageSettings.ContainerName = "providerversions";

                    return new BlobClient(storageSettings);
                });

            builder.AddSingleton<IVersionRepository<SpecificationVersion>, VersionRepository<SpecificationVersion>>((ctx) =>
            {
                CosmosDbSettings specsVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", specsVersioningDbSettings);

                specsVersioningDbSettings.ContainerName = "specs";

                CosmosRepository resultsRepostory = new CosmosRepository(specsVersioningDbSettings);

                return new VersionRepository<SpecificationVersion>(resultsRepostory);
            });

            builder.AddSingleton<ISpecificationsResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                Polly.Policy redisPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy);

                return new SpecificationsResiliencePolicies()
                {
                    PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CalcsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            MapperConfiguration mappingConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<SpecificationsMappingProfile>();
                c.AddProfile<PolicyMappingProfile>();
            });

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
            builder.AddCalculationsInterServiceClient(config);
            builder.AddProvidersInterServiceClient(config);
            builder.AddPoliciesInterServiceClient(config);
            builder.AddJobsInterServiceClient(config);
            builder.AddCalculationsInterServiceClient(config);

            builder.AddPolicySettings(config);

            builder.AddFeatureToggling(config);

            builder.AddApplicationInsightsForFunctionApps(config, "CalculateFunding.Functions.Specs");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Specs");
            builder.AddLogging("CalculateFunding.Functions.Specs");
            builder.AddTelemetry();

            return builder.BuildServiceProvider();
        }
    }
}
