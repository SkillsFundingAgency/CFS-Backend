using System;
using CalculateFunding.Functions.Publishing;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Functions.Publishing.ServiceBus;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Repositories;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

[assembly: FunctionsStartup(typeof(Startup))]

namespace CalculateFunding.Functions.Publishing
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

        public static IServiceProvider RegisterComponents(IServiceCollection builder,
            IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder,
            IConfigurationRoot config)
        {
            builder.AddCaching(config);
            builder.AddSingleton<OnRefreshFunding>();
            builder.AddSingleton<OnApproveFunding>();
            builder.AddSingleton<OnPublishFunding>();
            builder.AddSingleton<OnRefreshFundingFailure>();
            builder.AddSingleton<OnApproveFundingFailure>();
            builder.AddSingleton<OnPublishFundingFailure>();

            builder.AddSingleton<IRefreshService, RefreshService>();
            builder.AddSingleton<IApproveService, ApproveService>();
            builder.AddSingleton<IPublishService, PublishService>();

            builder.AddSingleton<ICalculationResultsRepository, CalculationResultsRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.CollectionName = "calculationresults";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);

                return new CalculationResultsRepository(calcsCosmosRepostory);
            });

            builder.AddSingleton<IPublishedResultService, PublishedResultService>();


            builder.AddSingleton<IJobHelperService, JobHelperService>();

            builder.AddApplicationInsights(config, "CalculateFunding.Functions.Publishing");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Publishing");

            builder.AddLogging("CalculateFunding.Functions.Publishing");

            builder.AddTelemetry();

            PolicySettings policySettings = builder.GetPolicySettings(config);
            ResiliencePolicies publishingResiliencePolicies = CreateResiliencePolicies(policySettings);

            builder.AddSingleton<IPublishingResiliencePolicies>(publishingResiliencePolicies);

            return builder.BuildServiceProvider();
        }

        private static ResiliencePolicies CreateResiliencePolicies(PolicySettings policySettings)
        {
            BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

            ResiliencePolicies resiliencePolicies = new ResiliencePolicies()
            {
                ResultsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy)
            };

            return resiliencePolicies;
        }
    }
}