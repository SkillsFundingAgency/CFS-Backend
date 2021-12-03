using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Filtering;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Serilog;


namespace CalculateFunding.Services.Publishing
{
    public class PublishedSearchService : SearchService<PublishedProviderIndex>, IPublishedSearchService, IHealthChecker
    {
        public static readonly FacetFilterType[] Facets = {
            new FacetFilterType("providerType"),
            new FacetFilterType("providerSubType"),
            new FacetFilterType("localAuthority"),
            new FacetFilterType("fundingStatus"),
            new FacetFilterType("indicative"),
            new FacetFilterType("monthYearOpened"),
            new FacetFilterType("hasErrors", fieldType: SearchFieldType.Boolean)
        };

        private static readonly string[] NonStringFields =
        {
            "fundingValue",
            "hasErrors",
        };

        private readonly IPublishedProvidersSearchService _publishedProvidersSearchService;
        private readonly ILogger _logger;

        public PublishedSearchService(ISearchRepository<PublishedProviderIndex> searchRepository,
            IPublishedProvidersSearchService publishedProvidersSearchService, ILogger logger)
            : base(searchRepository)
        {
            Guard.ArgumentNotNull(publishedProvidersSearchService, nameof(publishedProvidersSearchService));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _publishedProvidersSearchService = publishedProvidersSearchService;
            _logger = logger;
        }

        public async Task<IActionResult> SearchPublishedProviderLocalAuthorities(
            string searchText,
            string fundingStreamId,
            string fundingPeriodId)
        {
            string facetName = "localAuthority";

            FilterHelper filterHelper = new FilterHelper();
            AddFiltersForNotification(fundingStreamId, fundingPeriodId, filterHelper);

            SearchResults<PublishedProviderIndex> searchResults = await Task.Run(() =>
            {
                return SearchRepository.Search(string.Empty, new SearchParameters
                {
                    Facets = new[] { $"{facetName},count:1000" },
                    Top = 0,
                    Filter = filterHelper.BuildAndFilterQuery()
                });
            });

            IEnumerable<string> distinctFacetValues = searchResults
                .Facets
                .SingleOrDefault(x => x.Name == facetName)
                ?.FacetValues
                .Where(x => x.Name?.Split()
                    .Any(s => searchText == null || s.ToLowerInvariant().StartsWith(searchText.ToLowerInvariant())) == true)
                .Select(x => x.Name);

            return new OkObjectResult(distinctFacetValues);
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = await SearchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishedSearchService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = SearchRepository.GetType().GetFriendlyName(), Message = Message });

            return health;
        }

        public async Task<IActionResult> SearchPublishedProviderIds(PublishedProviderIdSearchModel searchModel)
        {
            if (searchModel == null)
            {
                _logger.Error("A null or invalid search model was provided for searching published provider ids");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            try
            {
                IDictionary<string, string[]> searchModelDictionary = searchModel.Filters;
                List<Filter> filters = searchModelDictionary
                    .Select(keyValueFilterPair => new Filter(
                        keyValueFilterPair.Key,
                        keyValueFilterPair.Value,
                        NonStringFields.Contains(keyValueFilterPair.Key),
                        "eq"))
                    .ToList();
                FilterHelper filterHelper = new FilterHelper(filters);

                IList<string> searchFields = searchModel.SearchFields?.ToList();
                int searchResultsBatchSize = 1000;
                IList<string> selectFields = new[] { "id" };
                IList<string> orderByFields = new[] { "id" };
                string filter = filterHelper.BuildAndFilterQuery();

                SearchResults<PublishedProviderIndex> searchResults = await Task.Run(() =>
                {
                    return SearchRepository.Search(searchModel.SearchTerm, new SearchParameters
                    {
                        Top = searchResultsBatchSize,
                        IncludeTotalResultCount = true,
                        Select = selectFields,
                        OrderBy = orderByFields,
                        SearchFields = searchFields,
                        Filter = filter
                    });
                });

                ConcurrentBag<string> results = new ConcurrentBag<string>();
                searchResults.Results.ForEach(_ => results.Add(_.Result.Id));

                if (searchResults.TotalCount > searchResultsBatchSize)
                {
                    SemaphoreSlim throttler = new SemaphoreSlim(10, 10);
                    List<Task> searchTasks = new List<Task>();
                    int count = searchResultsBatchSize;

                    while (count < searchResults.TotalCount)
                    {
                        SearchParameters searchParams = new SearchParameters
                        {
                            Skip = count,
                            Top = searchResultsBatchSize,
                            IncludeTotalResultCount = true,
                            Select = selectFields,
                            OrderBy = orderByFields,
                            SearchFields = searchFields,
                            Filter = filter
                        };

                        await throttler.WaitAsync();
                        searchTasks.Add(
                            Task.Run(async () =>
                            {
                                try
                                {
                                    SearchResults<PublishedProviderIndex> nextSearchResults =
                                        await SearchRepository.Search(searchModel.SearchTerm, searchParams);
                                    nextSearchResults.Results.ForEach(_ => results.Add(_.Result.Id));
                                }
                                finally
                                {
                                    throttler.Release();
                                }
                            }));

                        count += searchResultsBatchSize;
                    }

                    await TaskHelper.WhenAllAndThrow(searchTasks.ToArray());
                }

                return new OkObjectResult(results);
            }
            catch (FailedToQuerySearchException exception)
            {
                _logger.Error(exception, $"Failed to query search with term: {searchModel.SearchTerm}");

                return new InternalServerErrorResult($"Failed to query search, with exception: {exception.Message}");
            }
        }

        public async Task<IActionResult> SearchPublishedProviders(SearchModel searchModel)
        {
            if (searchModel == null || searchModel.PageNumber < 1 || searchModel.Top < 1)
            {
                _logger.Error("A null or invalid search model was provided for searching published providers");

                return new BadRequestObjectResult("An invalid search model was provided");
            }

            try
            {
                if (searchModel.OrderBy.IsNullOrEmpty())
                {
                    searchModel.OrderBy = new[] { "providerName" };
                }

                IEnumerable<Task<SearchResults<PublishedProviderIndex>>> searchTasks = await BuildSearchTasks(searchModel, Facets, true);

                if (searchTasks.IsNullOrEmpty())
                {
                    return new InternalServerErrorResult("Failed to build search tasks");
                }

                PublishedSearchResults results = new PublishedSearchResults();

                await TaskHelper.WhenAllAndThrow(searchTasks.ToArraySafe());

                foreach (Task<SearchResults<PublishedProviderIndex>> searchTask in searchTasks)
                {
                    SearchResults<PublishedProviderIndex> searchResult = searchTask.Result;

                    if (!searchResult.Facets.IsNullOrEmpty())
                    {
                        results.Facets = results.Facets.Concat(searchResult.Facets);
                    }
                    else
                    {
                        Dictionary<string, IEnumerable<ReleaseChannel>> releaseChannelLookupByProviderId =
                            await _publishedProvidersSearchService.GetPublishedProviderReleaseChannelsLookup(searchResult.Results?
                                .Select(_ => new ReleaseChannelSearch
                                {
                                    ProviderId = _.Result.UKPRN,
                                    SpecificationId = _.Result.SpecificationId,
                                    FundingStreamId = _.Result.FundingStreamId,
                                    FundingPeriodId = _.Result.FundingPeriodId
                                }));

                        results.TotalCount = (int)(searchResult.TotalCount ?? 0);
                        results.Results = searchResult.Results?.Select(m => new PublishedSearchResult
                        {
                            Id = m.Result.Id,
                            ProviderType = m.Result.ProviderType,
                            ProviderSubType = m.Result.ProviderSubType,
                            LocalAuthority = m.Result.LocalAuthority,
                            FundingStatus = m.Result.FundingStatus,
                            ProviderName = m.Result.ProviderName,
                            UKPRN = m.Result.UKPRN,
                            UPIN = m.Result.UPIN,
                            URN = m.Result.URN,
                            FundingValue = m.Result.FundingValue,
                            SpecificationId = m.Result.SpecificationId,
                            FundingStreamId = m.Result.FundingStreamId,
                            FundingPeriodId = m.Result.FundingPeriodId,
                            Indicative = m.Result.Indicative,
                            IsIndicative = m.Result.IsIndicative,
                            HasErrors = m.Result.HasErrors,
                            Errors = m.Result.Errors,
                            OpenedDate = m.Result.DateOpened,
                            MajorVersion = m.Result.MajorVersion,
                            MinorVersion = m.Result.MinorVersion,
                            ReleaseChannels = GetReleaseChannels(releaseChannelLookupByProviderId, m.Result.UKPRN)
                        });
                    }
                }

                return new OkObjectResult(results);
            }
            catch (FailedToQuerySearchException exception)
            {
                _logger.Error(exception, $"Failed to query search with term: {searchModel.SearchTerm}");

                return new InternalServerErrorResult($"Failed to query search, with exception: {exception.Message}");
            }
        }

        private IEnumerable<ReleaseChannel> GetReleaseChannels(
            Dictionary<string, IEnumerable<ReleaseChannel>> releaseChannelLookupByProviderId,
            string providerId)
        {
            if (releaseChannelLookupByProviderId.ContainsKey(providerId))
            {
                return releaseChannelLookupByProviderId[providerId];
            }

            return Enumerable.Empty<ReleaseChannel>();
        }

        private static void AddFiltersForNotification(string fundingStreamId, string fundingPeriodId, FilterHelper filterHelper)
        {
            if (!fundingStreamId.IsNullOrEmpty())
            {
                filterHelper.Filters.Add(new Filter("fundingStreamId", new[] { fundingStreamId }, false, "eq"));
            }

            if (!fundingPeriodId.IsNullOrEmpty())
            {
                filterHelper.Filters.Add(new Filter("fundingPeriodId", new[] { fundingPeriodId }, false, "eq"));
            }
        }
    }
}
