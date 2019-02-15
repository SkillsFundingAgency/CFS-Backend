using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Search;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Filtering;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.Search.Models;

namespace CalculateFunding.Services.Results
{
    public class AllocationNotificationsFeedsSearchService : IAllocationNotificationsFeedsSearchService, IHealthChecker
    {
        private readonly ISearchRepository<AllocationNotificationFeedIndex> _allocationNotificationsSearchRepository;
        private readonly Polly.Policy _allocationNotificationsSearchRepositoryPolicy;

        private IEnumerable<string> DefaultOrderBy = new[] { "dateUpdated desc" };

        public AllocationNotificationsFeedsSearchService(
            ISearchRepository<AllocationNotificationFeedIndex> allocationNotificationsSearchRepository,
            IResultsResilliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(allocationNotificationsSearchRepository, nameof(allocationNotificationsSearchRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));

            _allocationNotificationsSearchRepository = allocationNotificationsSearchRepository;
            _allocationNotificationsSearchRepositoryPolicy = resiliencePolicies.AllocationNotificationFeedSearchRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) searchRepoHealth = await _allocationNotificationsSearchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(CalculationProviderResultsSearchService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _allocationNotificationsSearchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });

            return health;
        }

		public async Task<SearchFeed<AllocationNotificationFeedIndex>> GetFeeds(int pageRef, int top = 500, IEnumerable<string> statuses = null)
		{
			if (pageRef < 1)
			{
				throw new ArgumentException("Page ref cannot be less than one", nameof(pageRef));
			}

			if (top < 1)
			{
				top = 500;
			}

			int skip = (pageRef - 1) * top;

			IList<string> filter = new List<string>();

			if (!statuses.IsNullOrEmpty())
			{
				if (!statuses.Contains("All"))
				{
					foreach (string status in statuses)
					{
						filter.Add($"allocationStatus eq '{status}'");
					}
				}
			}

			SearchResults<AllocationNotificationFeedIndex> searchResults = await _allocationNotificationsSearchRepositoryPolicy.ExecuteAsync(
				() => _allocationNotificationsSearchRepository.Search("", new SearchParameters
				{
					Skip = skip,
					Top = top,
					SearchMode = SearchMode.Any,
					IncludeTotalResultCount = true,
					Filter = filter.IsNullOrEmpty() ? "" : string.Join(" or ", filter),
					OrderBy = DefaultOrderBy.ToList(),
					QueryType = QueryType.Full
				}));

			return new SearchFeed<AllocationNotificationFeedIndex>
			{
				PageRef = pageRef,
				Top = top,
				TotalCount = searchResults != null && searchResults.TotalCount.HasValue ? (int)searchResults?.TotalCount : 0,
				Entries = searchResults?.Results.Select(m => m.Result)
			};
		}

	    public async Task<SearchFeedV2<AllocationNotificationFeedIndex>> GetFeedsV2(int? pageRef, int top, int? startYear = null, int? endYear = null, string ukprn = null, string laCode = null, bool? isAllocationLineContractRequired = null, IEnumerable<string> statuses = null, IEnumerable<string> fundingStreamIds = null, IEnumerable<string> allocationLineIds = null)
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
			AddFiltersForNotification(startYear, endYear, ukprn, laCode, isAllocationLineContractRequired, statuses, fundingStreamIds, allocationLineIds, filterHelper);

			if (pageRef == null)
			{
				SearchResults<AllocationNotificationFeedIndex> countSearchResults = await SearchResults(0, null, filterHelper.BuildAndFilterQuery());
				SearchFeedV2<AllocationNotificationFeedIndex> searchFeedCountResult = CreateSearchFeedResult(null, top, countSearchResults);
				pageRef = searchFeedCountResult.Last;
			}


			int skip = (pageRef.Value - 1) * top;

		    string filters = filterHelper.Filters.IsNullOrEmpty() ? "" : filterHelper.BuildAndFilterQuery();

			SearchResults<AllocationNotificationFeedIndex> searchResults = await SearchResults(top, skip, filters);

		    return CreateSearchFeedResult(pageRef, top, searchResults);
	    }

	    private static SearchFeedV2<AllocationNotificationFeedIndex> CreateSearchFeedResult(int? pageRef, int top, SearchResults<AllocationNotificationFeedIndex> searchResults)
	    {
		    SearchFeedV2<AllocationNotificationFeedIndex> searchFeedResult = new SearchFeedV2<AllocationNotificationFeedIndex>
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


	    public async Task<SearchFeed<AllocationNotificationFeedIndex>> GetFeeds(string providerId, int startYear, int endYear, IEnumerable<string> customFilters)
        {
            IList<string> filters = new List<string>();

            filters.Add($"fundingPeriodStartYear eq {startYear}");
            filters.Add($"fundingPeriodEndYear eq {endYear}");
            filters.Add($"providerId eq '{providerId}'");
			filters.Add($"allocationStatus eq 'Published'");

            return await SearchFeeds(startYear, endYear, filters, customFilters);
        }

        public async Task<SearchFeed<AllocationNotificationFeedIndex>> GetLocalAuthorityFeeds(string laCode, int startYear, int endYear, IEnumerable<string> customFilters)
        {
            IList<string> filters = new List<string>();

            filters.Add($"fundingPeriodStartYear eq {startYear}");
            filters.Add($"fundingPeriodEndYear eq {endYear}");
            filters.Add($"laCode eq '{laCode}'");
	        filters.Add("allocationStatus eq 'Published'");

			return await SearchFeeds(startYear, endYear, filters, customFilters);
        }

        private async Task<SearchFeed<AllocationNotificationFeedIndex>> SearchFeeds(int startYear, int endYear, IEnumerable<string> filters, IEnumerable<string> customFilters, int top = 500)
        {
            SearchResults<AllocationNotificationFeedIndex> searchResults = await _allocationNotificationsSearchRepositoryPolicy.ExecuteAsync(
                () => _allocationNotificationsSearchRepository.Search("", new SearchParameters
                {
                    Top = top,
                    SearchMode = SearchMode.Any,
                    IncludeTotalResultCount = true,
                    Filter = string.Join(" and ", filters) + (customFilters.IsNullOrEmpty() ? "" : " and (" + string.Join(" or ", customFilters) + ")"),
                    OrderBy = DefaultOrderBy.ToList(),
                    QueryType = QueryType.Full
                }));

            return new SearchFeed<AllocationNotificationFeedIndex>
            {
                Entries = searchResults?.Results.Select(m => m.Result)
            };
        }

	    private static void AddFiltersForNotification(int? startYear, int? endYear, string ukprn, string laCode, bool? isAllocationLineContractRequired, IEnumerable<string> statuses, IEnumerable<string> fundingStreamIds, IEnumerable<string> allocationLineIds,FilterHelper filterHelper)
	    {
		    if (!statuses.IsNullOrEmpty())
		    {
			    if (!statuses.Contains("All"))
			    {
				    filterHelper.Filters.Add(new Filter("allocationStatus", statuses, false, "eq"));
			    }
		    }

		    if (!fundingStreamIds.IsNullOrEmpty())
		    {
			    filterHelper.Filters.Add(new Filter("fundingStreamId", fundingStreamIds, false, "eq"));
		    }

		    if (!allocationLineIds.IsNullOrEmpty())
		    {
				filterHelper.Filters.Add(new Filter("allocationLineId", allocationLineIds, false, "eq")
				{
					Filters = allocationLineIds,
					Operator = "eq",
					FilterName = "allocationLineId"
				});
		    }

		    if (startYear.HasValue)
		    {
				filterHelper.Filters.Add(new Filter("fundingPeriodStartYear", new List<string>(){ startYear.Value.ToString()}, true, "eq")
				{
					Filters = new List<string>{startYear.Value.ToString()},
					Operator = "eq",
					FilterName = "fundingPeriodStartYear"
				});
		    }

		    if (endYear.HasValue)
		    {
				filterHelper.Filters.Add(new Filter("fundingPeriodEndYear", new List<string> { endYear.Value.ToString() }, true, "eq")
				{
					Filters = new List<string> { endYear.Value.ToString()},
					Operator = "eq",
					FilterName = "fundingPeriodEndYear"
				});
		    }

		    if (ukprn != null)
		    {
				filterHelper.Filters.Add(new Filter("providerUkprn", new List<string>(){ukprn}, false, "eq"));
		    }

		    if (laCode != null)
		    {
			    filterHelper.Filters.Add(new Filter("laCode", new List<string>() { laCode }, false, "eq"));
			}

		    if (isAllocationLineContractRequired.HasValue)
		    {
				filterHelper.Filters.Add(new Filter("allocationLineContractRequired", new List<string>(){ isAllocationLineContractRequired.ToString().ToLowerInvariant()}, true, "eq" ));
		    }
	    }

	    private async Task<SearchResults<AllocationNotificationFeedIndex>> SearchResults(int top, int? skip, string filters)
	    {
		    SearchResults<AllocationNotificationFeedIndex> searchResults =
			    await _allocationNotificationsSearchRepositoryPolicy.ExecuteAsync(
				    () =>
				    {
					    return _allocationNotificationsSearchRepository.Search("", new SearchParameters
					    {
						    Skip = skip,
						    Top = top,
						    SearchMode = SearchMode.Any,
						    IncludeTotalResultCount = true,
						    Filter = filters,
						    OrderBy = new[] { "dateUpdated asc" },
						    QueryType = QueryType.Full
					    });
				    });
		    return searchResults;
	    }
	}
}
