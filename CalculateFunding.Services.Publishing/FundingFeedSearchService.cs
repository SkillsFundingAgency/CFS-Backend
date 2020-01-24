using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.External;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Search;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Filtering;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.Search.Models;

namespace CalculateFunding.Services.Publishing
{
    public class FundingFeedSearchService : IFundingFeedSearchService, IHealthChecker
    {
        private readonly ISearchRepository<PublishedFundingIndex> _fundingSearchRepository;
        private readonly Polly.Policy _fundingSearchRepositoryPolicy;

        public FundingFeedSearchService(
            ISearchRepository<PublishedFundingIndex> fundingSearchRepository,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(fundingSearchRepository, nameof(fundingSearchRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.FundingFeedSearchRepository, nameof(resiliencePolicies.FundingFeedSearchRepository));

            _fundingSearchRepository = fundingSearchRepository;
            _fundingSearchRepositoryPolicy = resiliencePolicies.FundingFeedSearchRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) searchRepoHealth = await _fundingSearchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(FundingFeedSearchService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _fundingSearchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });

            return health;
        }

        public async Task<SearchFeedV3<PublishedFundingIndex>> GetFeedsV3(int? pageRef,
            int top,
            IEnumerable<string> fundingStreamIds = null,
            IEnumerable<string> fundingPeriodIds = null,
            IEnumerable<string> groupingReasons = null)
        {
            if (pageRef < 1)
            {
                throw new ArgumentException("Page ref cannot be less than one", nameof(pageRef));
            }

            if (top < 1)
            {
                top = 500;
            }

            FilterHelper filterHelper = new FilterHelper();
            AddFiltersForNotification(fundingStreamIds, fundingPeriodIds, groupingReasons, filterHelper);
            bool reverseOrder = false;

            if (pageRef == null)
            {
                SearchResults<PublishedFundingIndex> countSearchResults = await SearchResults(0, null, filterHelper.BuildAndFilterQuery());
                SearchFeedV3<PublishedFundingIndex> searchFeedCountResult = CreateSearchFeedResult(null, top, countSearchResults);
                pageRef = searchFeedCountResult.Last;
                reverseOrder = true;
            }

            int skip = (pageRef.Value - 1) * top;

            string filters = filterHelper.Filters.IsNullOrEmpty() ? "" : filterHelper.BuildAndFilterQuery();

            SearchResults<PublishedFundingIndex> searchResults = await SearchResults(top, skip, filters, reverseOrder);

            return CreateSearchFeedResult(pageRef, top, searchResults);
        }

        private static SearchFeedV3<PublishedFundingIndex> CreateSearchFeedResult(int? pageRef, int top, SearchResults<PublishedFundingIndex> searchResults)
        {
            SearchFeedV3<PublishedFundingIndex> searchFeedResult = new SearchFeedV3<PublishedFundingIndex>
            {
                Top = top,
                TotalCount = searchResults != null && searchResults.TotalCount.HasValue ? (int)searchResults?.TotalCount : 0,
                Entries = searchResults?.Results.Select(m => m.Result)
            };
            if (pageRef.HasValue)
            {
                searchFeedResult.PageRef = pageRef.Value;
            }
            return searchFeedResult;
        }

        private static void AddFiltersForNotification(IEnumerable<string> fundingStreamIds, IEnumerable<string> fundingPeriodIds, IEnumerable<string> groupingReasons, FilterHelper filterHelper)
        {
            if (!groupingReasons.IsNullOrEmpty())
            {
                if (!groupingReasons.Contains("All"))
                {
                    filterHelper.Filters.Add(new Filter("groupingType", groupingReasons, false, "eq"));
                }
            }

            if (!fundingStreamIds.IsNullOrEmpty())
            {
                filterHelper.Filters.Add(new Filter("fundingStreamId", fundingStreamIds, false, "eq"));
            }

            if (!fundingPeriodIds.IsNullOrEmpty())
            {
                filterHelper.Filters.Add(new Filter("fundingPeriodId", fundingPeriodIds, false, "eq"));
            }
        }

        private async Task<SearchResults<PublishedFundingIndex>> SearchResults(int top, int? skip, string filters, bool reverse = false)
        {
            SearchResults <PublishedFundingIndex> searchResults =
                await _fundingSearchRepositoryPolicy.ExecuteAsync(
                    () =>
                    {
                        return _fundingSearchRepository.Search("", new SearchParameters
                        {
                            Top = skip.HasValue ? (int?)null : top,
                            SearchMode = Microsoft.Azure.Search.Models.SearchMode.Any,
                            IncludeTotalResultCount = true,
                            Filter = filters,
                            OrderBy = new[] { "statusChangedDate", "id" },
                            QueryType = QueryType.Full
                        },
                        true);
                    });

            if(skip.HasValue)
            {
                searchResults?.Results?.RemoveRange(0, skip.Value);

                if (reverse)
                {
                    searchResults?.Results?.Reverse();
                }

                if (searchResults != null && !searchResults.Results.IsNullOrEmpty() && searchResults.Results.Count > top)
                {
                    searchResults.Results = searchResults.Results.Take(top).ToList();
                }
            }

            return searchResults;
        }
    }
}
