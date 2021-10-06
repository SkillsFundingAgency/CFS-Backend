using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using CalculateFunding.Common.Config.ApiClient.Dataset;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.Common.Config.ApiClient.Specifications;
using CalculateFunding.Common.Interfaces;
using CalculateFunding.Common.Models;
using CalculateFunding.Migrations.Specification.Clone.Clones;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;
using Serilog;
using System;
using System.Net.Http;
using System.Threading;

namespace CalculateFunding.Migrations.Specification.Clone
{
    internal class BootStrapper
    {
        private const string SourceCalcsClientKey = "SourceCalculations";
        private const string TargetCalcsClientKey = "TargetCalculations";
        private const string SourceSpecificationsClientKey = "SourceSpecifications";
        private const string TargetSpecificationsClientKey = "TargetSpecifications";
        private const string SourcePoliciesClientKey = "SourcePolicies";
        private const string TargetPoliciesClientKey = "TargetPolicies";
        private const string SourceDatasetsClientKey = "SourceDatasets";
        private const string TargetDatasetsClientKey = "TargetDatasets";
        private const string SourceJobsClientKey = "SourceJobs";
        private const string TargetJobsClientKey = "TargetJobs";


        private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
            .AddUserSecrets("96839a6b-8adf-4a77-8e64-8dd8331fd520")
            .Build();

        public static IServiceProvider BuildServiceProvider()
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Migrations.Specification.Clone");
            serviceCollection.AddLogging(serviceName: "CalculateFunding.Migrations.Specification.Clone");

            serviceCollection.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            serviceCollection.AddPolicySettings(Configuration);
            
            serviceCollection.AddJobsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan, clientKey: SourceJobsClientKey, clientName: "source:jobsClient");
            serviceCollection.AddJobsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan, clientKey: TargetJobsClientKey, clientName: "target:jobsClient");
            serviceCollection.AddDatasetsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan, clientKey: SourceDatasetsClientKey, clientName: "source:datasetsClient");
            serviceCollection.AddDatasetsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan, clientKey: TargetDatasetsClientKey, clientName: "target:datasetsClient");
            serviceCollection.AddPoliciesInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan, clientKey: SourcePoliciesClientKey, clientName: "source:policiesClient");
            serviceCollection.AddPoliciesInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan, clientKey: TargetPoliciesClientKey, clientName: "target:policiesClient");
            serviceCollection.AddCalculationsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan, clientKey: SourceCalcsClientKey, clientName: "source:CalcsClient");
            serviceCollection.AddCalculationsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan, clientKey: TargetCalcsClientKey, clientName: "target:CalcsClient");
            serviceCollection.AddSpecificationsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan, clientKey: SourceSpecificationsClientKey, clientName: "source:specificationsClient");
            serviceCollection.AddSpecificationsInterServiceClient(Configuration, handlerLifetime: Timeout.InfiniteTimeSpan, clientKey: TargetSpecificationsClientKey, clientName: "target:specificationsClient");


            serviceCollection.AddSingleton<IBatchCloneResiliencePolicies>(ctx =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();
                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new BatchCloneResiliencePolicies
                {
                    CalcsApiClient = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    SpecificationsApiClient = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    PoliciesApiClient = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    JobsApiClient = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    DatasetsApiClient = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                };
            });

            serviceCollection.AddSingleton<ISourceApiClient>(ctx =>
            {
                IBatchCloneResiliencePolicies batchCloneResiliencePolicies = ctx.GetService<IBatchCloneResiliencePolicies>();
                IHttpClientFactory httpClientFactory = ctx.GetService<IHttpClientFactory>();
                ILogger logger = ctx.GetService<ILogger>();
                ICancellationTokenProvider cancellationTokenProvider = ctx.GetService<ICancellationTokenProvider>();

                ISpecificationsApiClient specificationsApiClient = new SpecificationsApiClient(httpClientFactory, logger, cancellationTokenProvider, clientKey: SourceSpecificationsClientKey);
                ICalculationsApiClient calculationsApiClient = new CalculationsApiClient(httpClientFactory, logger, cancellationTokenProvider, clientKey: SourceCalcsClientKey);
                IDatasetsApiClient datasetsApiClient = new DatasetsApiClient(httpClientFactory, logger, cancellationTokenProvider, clientKey: SourceDatasetsClientKey);

                return new SourceApiClient(
                    batchCloneResiliencePolicies, 
                    specificationsApiClient,
                    calculationsApiClient,
                    datasetsApiClient,
                    logger);
            });

            serviceCollection.AddSingleton<ITargetApiClient>(ctx =>
            {
                IBatchCloneResiliencePolicies batchCloneResiliencePolicies = ctx.GetService<IBatchCloneResiliencePolicies>();
                IHttpClientFactory httpClientFactory = ctx.GetService<IHttpClientFactory>();
                ILogger logger = ctx.GetService<ILogger>();
                ICancellationTokenProvider cancellationTokenProvider = ctx.GetService<ICancellationTokenProvider>();

                ISpecificationsApiClient specificationsApiClient = new SpecificationsApiClient(httpClientFactory, logger, cancellationTokenProvider, clientKey: TargetSpecificationsClientKey);
                IJobsApiClient jobsApiClient = new JobsApiClient(httpClientFactory, logger, cancellationTokenProvider, clientKey: TargetJobsClientKey);
                ICalculationsApiClient calculationsApiClient = new CalculationsApiClient(httpClientFactory, logger, cancellationTokenProvider, clientKey: TargetCalcsClientKey);
                IDatasetsApiClient datasetsApiClient = new DatasetsApiClient(httpClientFactory, logger, cancellationTokenProvider, clientKey: TargetDatasetsClientKey);
                IPoliciesApiClient policiesApiClient = new PoliciesApiClient(httpClientFactory, logger, cancellationTokenProvider, clientKey: TargetPoliciesClientKey);

                return new TargetApiClient(
                    logger,
                    batchCloneResiliencePolicies,
                    specificationsApiClient,
                    jobsApiClient,
                    calculationsApiClient,
                    datasetsApiClient,
                    policiesApiClient);
            });

            serviceCollection.AddTransient<ISpecificationClone, SpecificationClone>();

            return serviceCollection.BuildServiceProvider();
        }

    }
}
