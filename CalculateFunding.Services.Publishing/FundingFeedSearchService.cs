using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.External;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing
{
    public class FundingFeedSearchService : IFundingFeedSearchService, IHealthChecker
    {
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly AsyncPolicy _publishedFundingRepositoryPolicy;

        public FundingFeedSearchService(IPublishedFundingRepository publishedFundingRepository, IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));

            _publishedFundingRepository = publishedFundingRepository;
            _publishedFundingRepositoryPolicy = resiliencePolicies.PublishedFundingRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth publishedFundingRepoHealth = await _publishedFundingRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(FundingFeedSearchService)
            };

            health.Dependencies.AddRange(publishedFundingRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth
            {
                HealthOk = publishedFundingRepoHealth.Dependencies.All(_ => _.HealthOk),
                DependencyName = publishedFundingRepoHealth.GetType().GetFriendlyName()
            });

            return health;
        }

        public async Task<SearchFeedResult<PublishedFundingIndex>> GetFeedsV3(int? pageRef,
            int top,
            IEnumerable<string> fundingStreamIds = null,
            IEnumerable<string> fundingPeriodIds = null,
            IEnumerable<string> groupingReasons = null,
            IEnumerable<string> variationReasons = null)
        {
            if (pageRef < 1)
            {
                throw new ArgumentException("Page ref cannot be less than one", nameof(pageRef));
            }

            if (top < 1)
            {
                top = 500;
            }

            int totalCount = await _publishedFundingRepositoryPolicy.ExecuteAsync(() => _publishedFundingRepository.QueryPublishedFundingCount(fundingStreamIds,
                fundingPeriodIds,
                groupingReasons,
                variationReasons));

            bool pageRefRequested = pageRef.HasValue;

            IEnumerable<PublishedFundingIndex> results = await _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                _publishedFundingRepository.QueryPublishedFunding(fundingStreamIds,
                    fundingPeriodIds,
                    groupingReasons,
                    variationReasons,
                    top,
                    pageRef,
                    totalCount));

            pageRef ??= new LastPage(totalCount, top);

            return CreateSearchFeedResult(pageRef.Value, top, totalCount, pageRefRequested, results);
        }

        private static SearchFeedResult<PublishedFundingIndex> CreateSearchFeedResult(int pageRef,
            int top,
            int totalCount,
            bool pageRefRequested,
            IEnumerable<PublishedFundingIndex> searchResults)
        {
            PublishedFundingIndex[] fundingFeedResults = pageRefRequested ? searchResults.ToArray() : searchResults.Reverse().ToArray();

            SearchFeedResult<PublishedFundingIndex> searchFeedResult = new SearchFeedResult<PublishedFundingIndex>
            {
                PageRef = pageRef,
                Top = top,
                TotalCount = totalCount,
                Entries = fundingFeedResults
            };

            return searchFeedResult;
        }
    }
}