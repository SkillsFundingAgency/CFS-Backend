using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingVersionDataService : IPublishedFundingVersionDataService
    {
        private readonly Policy _publishedFundingRepositoryPolicy;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly IPublishingEngineOptions _publishingEngineOptions;

        public PublishedFundingVersionDataService(
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPublishedFundingRepository publishedFundingRepository,
            IPublishingEngineOptions publishingEngineOptions)
        {
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishingEngineOptions, nameof(publishingEngineOptions));

            _publishedFundingRepositoryPolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _publishedFundingRepository = publishedFundingRepository;
            _publishingEngineOptions = publishingEngineOptions;
        }

        public async Task<IEnumerable<PublishedFundingVersion>> GetPublishedFundingVersion(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            ConcurrentBag<PublishedFundingVersion> results = new ConcurrentBag<PublishedFundingVersion>();

            IEnumerable<KeyValuePair<string, string>> publishedFundingIds = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                () => _publishedFundingRepository.GetPublishedFundingVersionIds(fundingStreamId, fundingPeriodId));

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.GetCurrentPublishedFundingConcurrencyCount);
            foreach (KeyValuePair<string, string> cosmosDocumentInformation in publishedFundingIds)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            PublishedFundingVersion result = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                () => _publishedFundingRepository.GetPublishedFundingVersionById(cosmosDocumentInformation.Key, cosmosDocumentInformation.Value));

                            if (result == null)
                            {
                                throw new InvalidOperationException($"PublishedFundingVersion not found for document '{cosmosDocumentInformation.Key}'");
                            }
                            results.Add(result);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            return results;
        }
    }
}
