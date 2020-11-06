using System;
using System.Threading;
using AutoMapper;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Providers;
using CalculateFunding.Common.Config.ApiClient.Scenarios;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Functions.TestEngine.ServiceBus;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.CodeMetadataGenerator;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Functions.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Logging;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.TestEngine.Interfaces;
using CalculateFunding.Services.TestEngine.MappingProfiles;
using CalculateFunding.Services.TestRunner;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.Repositories;
using CalculateFunding.Services.TestRunner.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Bulkhead;
using Serilog;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.TestEngine.Startup))]

namespace CalculateFunding.Functions.TestEngine
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterComponents(builder.Services, builder.GetFunctionsConfigurationToIncludeHostJson());
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfiguration azureFuncConfig = null)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig(azureFuncConfig);

            return RegisterComponents(builder, config);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder, IConfigurationRoot config)
        {
            // These registrations of the functions themselves are just for the DebugQueue. Ideally we don't want these registered in production
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddScoped<OnTestSpecificationProviderResultsCleanup>();
                builder.AddScoped<OnEditSpecificationEvent>();
                builder.AddScoped<OnTestExecution>();
                builder.AddScoped<OnDeleteTestResults>();
                builder.AddScoped<OnDeleteTestResultsFailure>();
            }

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder
                .AddSingleton<IGherkinParserService, GherkinParserService>();

            builder
               .AddSingleton<IGherkinParser, GherkinParser>();

            builder
                .AddSingleton<ICodeMetadataGeneratorService, ReflectionCodeMetadataGenerator>();

            builder
                .AddSingleton<IStepParserFactory, StepParserFactory>();

            builder.AddSingleton<ITestResultsRepository, TestResultsRepository>((ctx) =>
            {
                CosmosDbSettings testResultsDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", testResultsDbSettings);

                testResultsDbSettings.ContainerName = "testresults";

                CosmosRepository providersCosmosRepostory = new CosmosRepository(testResultsDbSettings);

                EngineSettings engineSettings = ctx.GetService<EngineSettings>();
                ILogger logger = ctx.GetService<ILogger>();

                return new TestResultsRepository(providersCosmosRepostory, logger, engineSettings);
            });

            builder
                .AddSingleton<IScenariosRepository, ScenariosRepository>();

            builder
                .AddScoped<ITestEngineService, TestEngineService>();

            builder
                .AddSingleton<ITestEngine, Services.TestRunner.TestEngine>();

            builder
               .AddSingleton<IGherkinExecutor, GherkinExecutor>();

            builder
                .AddSingleton<ICalculationsRepository, CalculationsRepository>();

            builder.AddSingleton<ICosmosRepository, CosmosRepository>();

            builder.AddSingleton<IProviderSourceDatasetsRepository, ProviderSourceDatasetsRepository>((ctx) =>
            {
                CosmosDbSettings providersDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", providersDbSettings);

                providersDbSettings.ContainerName = "providerdatasets";

                CosmosRepository providersCosmosRepostory = new CosmosRepository(providersDbSettings);

                EngineSettings engineSettings = ctx.GetService<EngineSettings>();

                return new ProviderSourceDatasetsRepository(providersCosmosRepostory, engineSettings);
            });

            builder.AddSingleton<IProviderResultsRepository, ProviderResultsRepository>((ctx) =>
            {
                CosmosDbSettings providersDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", providersDbSettings);

                providersDbSettings.ContainerName = "calculationresults";

                CosmosRepository providersCosmosRepostory = new CosmosRepository(providersDbSettings);

                return new ProviderResultsRepository(providersCosmosRepostory);
            });

            builder.AddSingleton<ITestResultsSearchService, TestResultsSearchService>();

            builder.AddSingleton<ITestResultsCountsService, TestResultsCountsService>();

            builder.AddSingleton<IJobManagement, JobManagement>();

            MapperConfiguration mappingConfiguration = new MapperConfiguration(c =>
            {
                c.AddProfile<TestEngineMappingProfile>();
            });

            builder
                .AddSingleton(mappingConfiguration.CreateMapper());

            builder.AddScoped<ITestResultsService, TestResultsService>();

            builder.AddSearch(config);
            builder
                .AddSingleton<ISearchRepository<TestScenarioResultIndex>, SearchRepository<TestScenarioResultIndex>>();

            builder.AddSpecificationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddScenariosInterServiceClient(config);
            builder.AddCalculationsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddProvidersInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddJobsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.TestEngine", TelemetryChannelType.Sync);
            builder.AddApplicationInsightsServiceName(config, "CalculateFunding.Functions.TestEngine");
            builder.AddLogging("CalculateFunding.Functions.TestEngine");

            builder.AddServiceBus(config, "testengine");

            builder.AddTelemetry();

            builder.AddEngineSettings(config);

            builder.AddPolicySettings(config);

            builder.AddFeatureToggling(config);

            builder.AddSingleton((Func<IServiceProvider, ITestRunnerResiliencePolicies>)((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                AsyncPolicy redisPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy);

                ResiliencePolicies resiliencePolicies = new ResiliencePolicies()
                {
                    CalculationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    CacheProviderRepository = redisPolicy,
                    ProviderResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    ProviderSourceDatasetsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    ScenariosRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(new[] { totalNetworkRequestsPolicy, redisPolicy }),
                    SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    TestResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    TestResultsSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy)
                };

                return resiliencePolicies;
            }));

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };

            });

            builder.AddScoped<IUserProfileProvider, UserProfileProvider>();

            return builder.BuildServiceProvider();
        }
    }
}
