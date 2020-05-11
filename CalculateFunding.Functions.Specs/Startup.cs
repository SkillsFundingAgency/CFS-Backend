using System;
using System.Threading;
using AutoMapper;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Providers;
using CalculateFunding.Common.Config.ApiClient.Results;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Schema10;
using CalculateFunding.Functions.Specs.ServiceBus;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
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
            builder.AddSingleton<IQueueCreateSpecificationJobActions, QueueCreateSpecificationJobAction>();
            builder.AddSingleton<IQueueDeleteSpecificationJobActions, QueueDeleteSpecificationJobAction>();

            // These registrations of the functions themselves are just for the DebugQueue. Ideally we don't want these registered in production
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddScoped<OnAddRelationshipEvent>();
            }

            builder.AddSingleton<ISpecificationsRepository, SpecificationsRepository>((ctx) =>
            {
                CosmosDbSettings specsVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", specsVersioningDbSettings);

                specsVersioningDbSettings.ContainerName = "specs";

                CosmosRepository resultsRepostory = new CosmosRepository(specsVersioningDbSettings);

                return new SpecificationsRepository(resultsRepostory);
            });

            builder.AddSingleton<ISpecificationsService, SpecificationsService>();
            builder.AddSingleton<IValidator<SpecificationCreateModel>, SpecificationCreateModelValidator>();
            builder.AddSingleton<IValidator<SpecificationEditModel>, SpecificationEditModelValidator>();
            builder.AddSingleton<IValidator<AssignDefinitionRelationshipMessage>, AssignDefinitionRelationshipMessageValidator>();
            builder.AddSingleton<ISpecificationsSearchService, SpecificationsSearchService>();
            builder.AddSingleton<IResultsRepository, ResultsRepository>();            

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

            builder.AddSingleton<IVersionRepository<Models.Specs.SpecificationVersion>, VersionRepository<Models.Specs.SpecificationVersion>>((ctx) =>
            {
                CosmosDbSettings specsVersioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", specsVersioningDbSettings);

                specsVersioningDbSettings.ContainerName = "specs";

                CosmosRepository resultsRepostory = new CosmosRepository(specsVersioningDbSettings);

                return new VersionRepository<Models.Specs.SpecificationVersion>(resultsRepostory);
            });

            builder
                .AddSingleton<IJobManagement, JobManagement>();

            PolicySettings policySettings = builder.GetPolicySettings(config);

            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            builder.AddSingleton<ISpecificationsResiliencePolicies>((ctx) =>
            {
                Polly.AsyncPolicy redisPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy);

                return new SpecificationsResiliencePolicies()
                {
                    PoliciesApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CalcsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                  ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };

            });

            MapperConfiguration mappingConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<SpecificationsMappingProfile>();             
            });

            builder.AddSingleton(mappingConfig.CreateMapper());

            builder.AddServiceBus(config, "specs");

            builder.AddSearch(config);
            builder
             .AddSingleton<ISearchRepository<SpecificationIndex>, SearchRepository<SpecificationIndex>>();

            builder.AddCaching(config);


            builder.AddResultsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddProvidersInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddPoliciesInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddJobsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddCalculationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);

            builder.AddPolicySettings(config);

            builder.AddFeatureToggling(config);

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Specs");
            builder.AddApplicationInsightsServiceName(config, "CalculateFunding.Functions.Specs");
            builder.AddLogging("CalculateFunding.Functions.Specs");
            builder.AddTelemetry();

            return builder.BuildServiceProvider();
        }
    }
}
