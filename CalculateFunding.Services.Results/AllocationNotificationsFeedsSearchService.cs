using CalculateFunding.Models.Health;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Search;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class AllocationNotificationsFeedsSearchService : IAllocationNotificationsFeedsSearchService, IHealthChecker
    {
        private readonly ISearchRepository<AllocationNotificationFeedIndex> _allocationNotificationsSearchRepository;
        private readonly Polly.Policy _allocationNotificationsSearchRepositoryPolicy;

        private IEnumerable<string> DefaultOrderBy = new[] { "dateUpdated desc" };

        public AllocationNotificationsFeedsSearchService (
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
            var searchRepoHealth = await _allocationNotificationsSearchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(CalculationProviderResultsSearchService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _allocationNotificationsSearchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });

            return health;
        }

        public async Task<SearchFeed<AllocationNotificationFeedIndex>> GetFeeds(int pageRef, int top = 500, IEnumerable<string> statuses = null)
        {
            if(pageRef < 1)
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

        public async Task<SearchFeed<AllocationNotificationFeedIndex>> GetFeeds(string providerId, int startYear, int endYear, IEnumerable<string> customFilters)
        {
            IList<string> filters = new List<string>();

            filters.Add($"fundingPeriodStartYear eq {startYear}");
            filters.Add($"fundingPeriodEndYear eq {endYear}");
            filters.Add($"providerId eq '{providerId}'");

            return await SearchFeeds(startYear, endYear, filters, customFilters);
        }

        public async Task<SearchFeed<AllocationNotificationFeedIndex>> GetLocalAuthorityFeeds(string laCode, int startYear, int endYear, IEnumerable<string> customFilters)
        {
            IList<string> filters = new List<string>();

            filters.Add($"fundingPeriodStartYear eq {startYear}");
            filters.Add($"fundingPeriodEndYear eq {endYear}");
            filters.Add($"laCode eq '{laCode}'");

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
    }
}
