using System;
using System.Threading;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Functions.Policy;
using CalculateFunding.Functions.Policy.ServiceBus;
using CalculateFunding.Models.Policy;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Functions.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.DeadletterProcessor;
using CalculateFunding.Services.Policy;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.TemplateBuilder;
using CalculateFunding.Services.Policy.Validators;
using CalculateFunding.Services.Processing.Interfaces;
using CalculateFunding.Services.Providers.Validators;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Polly.Bulkhead;
using ServiceCollectionExtensions = CalculateFunding.Services.Core.Extensions.ServiceCollectionExtensions;

[assembly: FunctionsStartup(typeof(Startup))]
namespace CalculateFunding.Functions.Policy
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
            builder.AddAppConfiguration();
            builder.AddSingleton<IConfiguration>(config);

            builder.AddFeatureManagement();

            // These registrations of the functions themselves are just for the DebugQueue. Ideally we don't want these registered in production
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddScoped<OnReIndexTemplates>();
            }

            PolicySettings policySettings = ServiceCollectionExtensions.GetPolicySettings(config);
            PolicyResiliencePolicies policyResiliencePolicies = CreateResiliencePolicies(policySettings);

            builder.AddSingleton<IJobManagementResiliencePolicies>((ctx) =>
            {
                return new JobManagementResiliencePolicies()
                {
                    JobsApiClient = policyResiliencePolicies.JobsApiClient
                };

            });
            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder
                .AddSingleton<IFundingStreamService, FundingStreamService>()
                .AddSingleton<IHealthChecker, FundingStreamService>()
                .AddSingleton<IValidator<FundingStreamSaveModel>, FundingStreamSaveModelValidator>();

            builder
                .AddSingleton<IFundingPeriodService, FundingPeriodService>()
                .AddSingleton<IHealthChecker, FundingPeriodService>()
                .AddSingleton<IFundingPeriodValidator, FundingPeriodValidator>();

            builder.AddSingleton<IValidator<FundingPeriodsJsonModel>, FundingPeriodJsonModelValidator>();

            builder.AddSingleton<ICosmosRepository, CosmosRepository>();
            builder.AddSingleton<ITemplatesReIndexerService, TemplatesReIndexerService>();
            builder.AddCaching(config);
            builder.AddSearch(config);
            builder
                .AddSingleton<ISearchRepository<TemplateIndex>, SearchRepository<TemplateIndex>>();
            builder.AddSingleton<IPolicyRepository, PolicyRepository>((ctx) =>
            {
                CosmosDbSettings policyDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", policyDbSettings);

                policyDbSettings.ContainerName = "policy";

                CosmosRepository policyCosmosRepository = new CosmosRepository(policyDbSettings);

                return new PolicyRepository(policyCosmosRepository);
            });

            builder.AddSingleton<ITemplateRepository, TemplateRepository>((ctx) =>
            {
                CosmosDbSettings policyDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", policyDbSettings);

                policyDbSettings.ContainerName = "templatebuilder";

                CosmosRepository policyCosmosRepository = new CosmosRepository(policyDbSettings);

                return new TemplateRepository(policyCosmosRepository);
            });

            builder.AddServiceBus(config, "policy");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Policy");
            builder.AddApplicationInsightsServiceName(config, "CalculateFunding.Functions.Policy");
            builder.AddLogging("CalculateFunding.Functions.Policy", config);
            builder.AddTelemetry();

            builder.AddSingleton<IPolicyResiliencePolicies>(policyResiliencePolicies);
            builder.AddSingleton<IDeadletterService, DeadletterService>();
            builder.AddSingleton<IJobManagement, JobManagement>();
            builder.AddSingleton<ITemplatesReIndexerService, TemplatesReIndexerService>();

            builder.AddJobsInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);
            builder.AddPoliciesInterServiceClient(config, handlerLifetime: Timeout.InfiniteTimeSpan);

            return builder.BuildServiceProvider();
        }

        private static PolicyResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            return new PolicyResiliencePolicies
            {
                PolicyRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                CacheProvider = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                FundingSchemaRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                FundingTemplateRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                TemplatesSearchRepository = SearchResiliencePolicyHelper.GenerateSearchPolicy(totalNetworkRequestsPolicy),
                JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                TemplatesRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                SpecificationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                ResultsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                CalculationsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
            };
        }
    }
}
