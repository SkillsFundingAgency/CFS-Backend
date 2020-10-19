using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using ApiFundingPeriod = CalculateFunding.Common.ApiClient.Policies.Models.FundingPeriod;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingDataService : IPublishedFundingDataService
    {
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly ISpecificationService _specificationService;
        private readonly AsyncPolicy _publishedFundingRepositoryPolicy;
        private readonly AsyncPolicy _specificationsRepositoryPolicy;
        private readonly IPoliciesService _policiesService;
        private readonly IPublishingEngineOptions _publishingEngineOptions;

        public PublishedFundingDataService(
            IPublishedFundingRepository publishedFundingRepository,
            ISpecificationService specificationService,
            IPublishingResiliencePolicies publishingResiliencePolicies, 
            IPublishingEngineOptions publishingEngineOptions,
            IPoliciesService policiesService)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies.SpecificationsRepositoryPolicy, nameof(publishingResiliencePolicies.SpecificationsRepositoryPolicy));
            Guard.ArgumentNotNull(publishingEngineOptions, nameof(publishingEngineOptions));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));

            _publishedFundingRepository = publishedFundingRepository;
            _specificationService = specificationService;
            _publishingEngineOptions = publishingEngineOptions;
            _publishedFundingRepositoryPolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _specificationsRepositoryPolicy = publishingResiliencePolicies.SpecificationsRepositoryPolicy;
            _policiesService = policiesService;
        }

        public async Task<IEnumerable<PublishedProvider>> GetPublishedProvidersForApproval(string specificationId, string[] publishedProviderIds = null)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            SpecificationSummary specificationSummary = await _specificationsRepositoryPolicy.ExecuteAsync(
                () => _specificationService.GetSpecificationSummaryById(specificationId));

            ConcurrentBag<PublishedProvider> results = new ConcurrentBag<PublishedProvider>();

            string fundingPeriodId = specificationSummary?.FundingPeriod?.Id;

            if (fundingPeriodId.IsNullOrWhitespace())
            {
                string error = "Could not locate a funding period from the supplied funding period id on the specification summary";
                throw new InvalidOperationException(error);
            }

            foreach (Common.Models.Reference fundingStream in specificationSummary.FundingStreams)
            {
                IEnumerable<KeyValuePair<string, string>> publishedProviders = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                    () => _publishedFundingRepository.GetPublishedProviderIdsForApproval(fundingStream.Id, fundingPeriodId, publishedProviderIds));

                List<Task> allTasks = new List<Task>();
                SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.GetPublishedProvidersForApprovalConcurrencyCount);
                foreach (var cosmosDocumentInformation in publishedProviders)
                {
                    await throttler.WaitAsync();
                    allTasks.Add(
                        Task.Run(async () =>
                        {
                            try
                            {
                                PublishedProvider result = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                    () => _publishedFundingRepository.GetPublishedProviderById(cosmosDocumentInformation.Key, cosmosDocumentInformation.Value));

                                if (result == null)
                                {
                                    throw new InvalidOperationException($"PublishedProvider not found for document '{cosmosDocumentInformation.Key}'");
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
            }

            return results;
        }

        public async Task<IEnumerable<PublishedFunding>> GetCurrentPublishedFunding(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            ConcurrentBag<PublishedFunding> results = new ConcurrentBag<PublishedFunding>();

            IEnumerable<KeyValuePair<string, string>> publishedFundingIds = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                () => _publishedFundingRepository.GetPublishedFundingIds(fundingStreamId, fundingPeriodId));

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
                            PublishedFunding result = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                () => _publishedFundingRepository.GetPublishedFundingById(cosmosDocumentInformation.Key, cosmosDocumentInformation.Value));

                            if (result == null)
                            {
                                throw new InvalidOperationException($"PublishedFunding not found for document '{cosmosDocumentInformation.Key}'");
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

        public async Task<IEnumerable<PublishedFunding>> GetCurrentPublishedFunding(string specificationId, GroupingReason? groupingReason = null)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ConcurrentBag<PublishedFunding> results = new ConcurrentBag<PublishedFunding>();

            IEnumerable<KeyValuePair<string, string>> publishedFundingIds = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                () => _publishedFundingRepository.GetPublishedFundingIds(specificationId, groupingReason));

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
                            PublishedFunding result = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                () => _publishedFundingRepository.GetPublishedFundingById(cosmosDocumentInformation.Key, cosmosDocumentInformation.Value));

                            if (result == null)
                            {
                                throw new InvalidOperationException($"PublishedFunding not found for document '{cosmosDocumentInformation.Key}'");
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

        public async Task<IEnumerable<PublishedProvider>> GetCurrentPublishedProviders(string fundingStreamId, string fundingPeriodId, string[] providerIds = null)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            ConcurrentBag<PublishedProvider> results = new ConcurrentBag<PublishedProvider>();

            IEnumerable<KeyValuePair<string, string>> publishedProviderIds = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                () => _publishedFundingRepository.GetPublishedProviderIds(fundingStreamId, fundingPeriodId, providerIds));

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.GetCurrentPublishedProvidersConcurrencyCount);
            foreach (var cosmosDocumentInformation in publishedProviderIds)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            PublishedProvider result = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                () => _publishedFundingRepository.GetPublishedProviderById(cosmosDocumentInformation.Key, cosmosDocumentInformation.Value));

                            if (result == null)
                            {
                                throw new InvalidOperationException($"PublishedProvider not found for document '{cosmosDocumentInformation.Key}'");
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

        public async Task<IEnumerable<string>> GetPublishedProviderFundingLines(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            IEnumerable<string> result = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                () => _publishedFundingRepository.GetPublishedProviderFundingLines(specificationId, GroupingReason.Payment));

            return result;
        }
    }
}
