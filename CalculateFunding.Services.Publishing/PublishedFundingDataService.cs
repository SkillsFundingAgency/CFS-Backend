using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingDataService : IPublishedFundingDataService
    {
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly ISpecificationService _specificationService;
        private readonly AsyncPolicy _publishedFundingRepositoryPolicy;
        private readonly AsyncPolicy _specificationsRepositoryPolicy;
        private readonly IPublishedFundingBulkRepository _publishedFundingBulkRepository;

        public PublishedFundingDataService(
            IPublishedFundingRepository publishedFundingRepository,
            ISpecificationService specificationService,
            IPublishingResiliencePolicies publishingResiliencePolicies, 
            IPublishedFundingBulkRepository publishedFundingBulkRepository)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies.SpecificationsRepositoryPolicy, nameof(publishingResiliencePolicies.SpecificationsRepositoryPolicy));
            Guard.ArgumentNotNull(publishedFundingBulkRepository, nameof(publishedFundingBulkRepository));

            _publishedFundingRepository = publishedFundingRepository;
            _specificationService = specificationService;
            _publishedFundingRepositoryPolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _specificationsRepositoryPolicy = publishingResiliencePolicies.SpecificationsRepositoryPolicy;
            _publishedFundingBulkRepository = publishedFundingBulkRepository;
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

            List<KeyValuePair<string, string>> allPublishedProviderIds = new List<KeyValuePair<string, string>>();

            foreach (Common.Models.Reference fundingStream in specificationSummary.FundingStreams)
            {
                IEnumerable<KeyValuePair<string, string>> publishedProviders = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                    () => _publishedFundingRepository.GetPublishedProviderIdsForApproval(fundingStream.Id, fundingPeriodId, publishedProviderIds));

                allPublishedProviderIds.AddRange(publishedProviders);
            }

            return await _publishedFundingBulkRepository.GetPublishedProviders(allPublishedProviderIds);
        }

        public async Task<IEnumerable<PublishedFunding>> GetCurrentPublishedFunding(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            IEnumerable<KeyValuePair<string, string>> publishedFundingIds = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                () => _publishedFundingRepository.GetPublishedFundingIds(fundingStreamId, fundingPeriodId));

            return await _publishedFundingBulkRepository.GetPublishedFundings(publishedFundingIds);
        }

        public async Task<IEnumerable<PublishedFunding>> GetCurrentPublishedFunding(string specificationId, GroupingReason? groupingReason = null)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            IEnumerable<KeyValuePair<string, string>> publishedFundingIds = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                () => _publishedFundingRepository.GetPublishedFundingIds(specificationId, groupingReason));

            return await _publishedFundingBulkRepository.GetPublishedFundings(publishedFundingIds);
        }

        public async Task<IEnumerable<PublishedProvider>> GetCurrentPublishedProviders(string fundingStreamId, string fundingPeriodId, string[] providerIds = null)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            IEnumerable<KeyValuePair<string, string>> publishedProviderIds = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                () => _publishedFundingRepository.GetPublishedProviderIds(fundingStreamId, fundingPeriodId, providerIds));

            return await _publishedFundingBulkRepository.GetPublishedProviders(publishedProviderIds);
        }

        public async Task<IEnumerable<(string Code, string Name)>> GetPublishedProviderFundingLines(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            IEnumerable<(string Code, string Name)> result = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                () => _publishedFundingRepository.GetPublishedProviderFundingLines(specificationId, GroupingReason.Payment));

            return result;
        }

        public async Task DeletePublishedProviders(IEnumerable<PublishedProvider> publishedProviders)
        {
            Guard.IsNotEmpty(publishedProviders, nameof(publishedProviders));

            await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                () => _publishedFundingRepository.DeletePublishedProviders(publishedProviders));
        }
    }
}
