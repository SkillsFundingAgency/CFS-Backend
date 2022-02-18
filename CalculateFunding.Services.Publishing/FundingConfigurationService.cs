using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;

namespace CalculateFunding.Services.Publishing
{
    public class FundingConfigurationService : IFundingConfigurationService
    {
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly AsyncPolicy _publishingResiliencePolicy;

        public FundingConfigurationService(
            IPoliciesApiClient policiesApiClient,
            IPublishingResiliencePolicies publishingResiliencePolicies)
        {
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));

            _policiesApiClient = policiesApiClient;
            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
        }

        public async Task<IDictionary<string, FundingConfiguration>> GetFundingConfigurations(SpecificationSummary specificationSummary)
        {
            Guard.ArgumentNotNull(specificationSummary, nameof(specificationSummary));

            int fundingStreamCount = specificationSummary.FundingStreams.Count();

            IList<Task> allTasks = new List<Task>();

            SemaphoreSlim throttler = new SemaphoreSlim(fundingStreamCount);

            ConcurrentDictionary<string, FundingConfiguration> fundingConfigurations = new ConcurrentDictionary<string, FundingConfiguration>();

            IEnumerable<string> fundingStreamIds = specificationSummary.FundingStreams.Select(m => m.Id);

            foreach (String fundingStreamId in fundingStreamIds)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            ApiResponse<FundingConfiguration> fundingConfigurationResponse =
                                await _publishingResiliencePolicy.ExecuteAsync(() => _policiesApiClient.GetFundingConfiguration(fundingStreamId, specificationSummary.FundingPeriod.Id));

                            if (fundingConfigurationResponse == null || !fundingConfigurationResponse.StatusCode.IsSuccess())
                            {
                                throw new Exception($"Failed to retrieve funding configuration for funding stream id '{fundingStreamId}' and period id '{specificationSummary.FundingPeriod?.Id}'");
                            }

                            fundingConfigurations.TryAdd(fundingStreamId, fundingConfigurationResponse.Content);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }

            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            return fundingConfigurations;
        }
    }
}
