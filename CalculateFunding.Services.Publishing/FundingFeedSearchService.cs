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
using CalculateFunding.Services.Publising.Interfaces;
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
            IEnumerable<string> groupingReasons = null,
            params string[] orderBy)
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

            if (pageRef == null)
            {
                SearchResults<PublishedFundingIndex> countSearchResults = await SearchResults(0, null, null, filterHelper.BuildAndFilterQuery(), orderBy);
                SearchFeedV3<PublishedFundingIndex> searchFeedCountResult = CreateSearchFeedResult(null, top, countSearchResults);
                pageRef = searchFeedCountResult.Last;
            }

            int skip = (pageRef.Value - 1) * top;

            string filters = filterHelper.Filters.IsNullOrEmpty() ? "" : filterHelper.BuildAndFilterQuery();

            SearchResults<PublishedFundingIndex> searchResults = await SearchResults(top, skip, pageRef, filters, orderBy);

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

        private async Task<SearchResults<PublishedFundingIndex>> SearchResults(int top, int? skip, int? pageRef, string filters, params string[] orderBy)
        {
            string statusChangedDateOrderByParameter = pageRef == null ? "statusChangedDate desc" : "statusChangedDate asc";

            SearchResults <PublishedFundingIndex> searchResults =
                await _fundingSearchRepositoryPolicy.ExecuteAsync(
                    () =>
                    {
                        return _fundingSearchRepository.Search("", new SearchParameters
                        {
                            Skip = skip,
                            Top = top,
                            SearchMode = Microsoft.Azure.Search.Models.SearchMode.Any,
                            IncludeTotalResultCount = true,
                            Filter = filters,
                            OrderBy = orderBy?.Any() == false ? new[] { statusChangedDateOrderByParameter, "id asc" } : orderBy,
                            QueryType = QueryType.Full
                        });
                    });
            return searchResults;
        }
    }
}
