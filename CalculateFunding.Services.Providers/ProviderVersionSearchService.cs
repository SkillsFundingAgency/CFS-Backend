using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Providers
{
    public class ProviderVersionSearchService : IProviderVersionSearchService, IHealthChecker
    {
        private readonly Policy _searchRepositoryPolicy;
        private readonly ILogger _logger;
        private readonly ISearchRepository<ProvidersIndex> _searchRepository;
        private readonly Policy _providerVersionMetadataRepositoryPolicy;
        private readonly IProviderVersionsMetadataRepository _providerVersionMetadataRepository;
        private readonly IProviderVersionService _providerVersionService;

        private readonly FacetFilterType[] Facets = {
            new FacetFilterType("providerType"),
            new FacetFilterType("providerSubType"),
            new FacetFilterType("authority"),
            new FacetFilterType("providerId"),
            new FacetFilterType("providerVersionId")
        };

        public ProviderVersionSearchService(ILogger logger,
            ISearchRepository<ProvidersIndex> searchRepository,
            IProviderVersionsMetadataRepository providerVersionMetadataRepository,
            IProvidersResiliencePolicies resiliencePolicies,
            IProviderVersionService providerVersionService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));

            _logger = logger;
            _searchRepository = searchRepository;
            _searchRepositoryPolicy = resiliencePolicies.ProviderVersionsSearchRepository;
            _providerVersionMetadataRepository = providerVersionMetadataRepository;
            _providerVersionMetadataRepositoryPolicy = resiliencePolicies.ProviderVersionMetadataRepository;
            _providerVersionService = providerVersionService;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = await _searchRepository.IsHealthOk();

            ServiceHealth providerVersionMetadataRepoHealth = await ((IHealthChecker)_providerVersionMetadataRepository).IsHealthOk();

            ServiceHealth providerVersionServiceHealth = await _providerVersionService.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderVersionSearchService)
            };

            health.Dependencies.AddRange(providerVersionMetadataRepoHealth.Dependencies);
            health.Dependencies.AddRange(providerVersionServiceHealth.Dependencies);

            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = Message });

            return health;
        }

        public async Task<IActionResult> GetProviderById(string providerVersionId, string providerId)
        {
            SearchModel searchModel = new SearchModel { Top = 1 };
            searchModel.Filters = new Dictionary<string, string[]>
            {
                { "providerId", new string[] { providerId  } },
                { "providerVersionId" , new string[] { providerVersionId } }
            };

            try
            {
                ProviderVersionSearchResults results = await SearchProviderVersionSearchResults(searchModel);
                if (results.Results.IsNullOrEmpty())
                {
                    return new NotFoundResult();
                }

                return new OkObjectResult(results.Results.First());
            }
            catch (FailedToQuerySearchException exception)
            {
                string error = $"Failed to query search with Provider Version Id: {providerVersionId} and Provider Id: {providerId}";

                _logger.Error(exception, error);

                return new InternalServerErrorResult(error);
            }
        }

        public async Task<IActionResult> GetProviderById(int year, int month, int day, string providerId)
        {
            Guard.ArgumentNotNull(day, nameof(day));
            Guard.ArgumentNotNull(month, nameof(month));
            Guard.ArgumentNotNull(year, nameof(year));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            ProviderVersionByDate providerVersionByDate = await _providerVersionService.GetProviderVersionByDate(year, month, day);

            if (providerVersionByDate != null)
            {
                return await GetProviderById(providerVersionByDate.ProviderVersionId, providerId);
            }

            return new NotFoundResult();
        }

        public async Task<IActionResult> SearchProviders(string providerVersionId, SearchModel searchModel = null)
        {
            searchModel = searchModel ?? new SearchModel();

            if (!searchModel.Filters.ContainsKey("providerVersionId"))
            {
                searchModel.Filters.Add("providerVersionId", new string[] { providerVersionId });
            }

            try
            {
                ProviderVersionSearchResults results = await SearchProviderVersionSearchResults(searchModel);
                return new OkObjectResult(results);
            }
            catch (FailedToQuerySearchException exception)
            {
                string error = $"Failed to query search with Provider Version Id: {providerVersionId} and Search Term: {searchModel.SearchTerm}";

                _logger.Error(exception, error);

                return new InternalServerErrorResult(error);
            }
        }

        public async Task<IActionResult> GetProviderByIdFromMaster(string providerId)
        {
            MasterProviderVersion masterProviderVersion = await _providerVersionService.GetMasterProviderVersion();

            if (masterProviderVersion != null)
            {
                return await GetProviderById(masterProviderVersion.ProviderVersionId, providerId);
            }

            return new NotFoundResult();
        }

        public async Task<IActionResult> SearchMasterProviders(SearchModel searchModel)
        {
            MasterProviderVersion masterProviderVersion = await _providerVersionService.GetMasterProviderVersion();

            if (masterProviderVersion != null)
            {
                return await SearchProviders(masterProviderVersion.ProviderVersionId, searchModel);
            }

            return new NotFoundResult();
        }

        public async Task<IActionResult> SearchProviders(int year, int month, int day, SearchModel searchModel)
        {
            Guard.ArgumentNotNull(day, nameof(day));
            Guard.ArgumentNotNull(month, nameof(month));
            Guard.ArgumentNotNull(year, nameof(year));
            Guard.ArgumentNotNull(searchModel, nameof(searchModel));

            ProviderVersionByDate providerVersionByDate = await _providerVersionService.GetProviderVersionByDate(year, month, day);

            if (providerVersionByDate != null)
            {
                return await SearchProviders(providerVersionByDate.ProviderVersionId, searchModel);
            }

            return new NotFoundResult();
        }

        public async Task<IActionResult> SearchProviderVersions(SearchModel searchModel)
        {
            Guard.ArgumentNotNull(searchModel, nameof(searchModel));

            try
            {
                ProviderVersionSearchResults results = await SearchProviderVersionSearchResults(searchModel);
                return new OkObjectResult(results);
            }
            catch (FailedToQuerySearchException exception)
            {
                string error = $"Failed to query search with term: {searchModel.SearchTerm}";

                _logger.Error(exception, error);

                return new InternalServerErrorResult(error);
            }
        }

        private async Task<ProviderVersionSearchResults> SearchProviderVersionSearchResults(SearchModel searchModel)
        {
            IEnumerable<Task<SearchResults<ProvidersIndex>>> searchTasks = BuildSearchTasks(searchModel);

            await TaskHelper.WhenAllAndThrow(searchTasks.ToArraySafe());

            ProviderVersionSearchResults results = new ProviderVersionSearchResults();
            foreach (var searchTask in searchTasks)
            {
                ProcessSearchResults(searchTask.Result, results);
            }

            return results;
        }

        private IEnumerable<Task<SearchResults<ProvidersIndex>>> BuildSearchTasks(SearchModel searchModel)
        {
            IDictionary<string, string> facetDictionary = BuildFacetDictionary(searchModel);

            IEnumerable<Task<SearchResults<ProvidersIndex>>> searchTasks = new Task<SearchResults<ProvidersIndex>>[0];

            if (searchModel.IncludeFacets)
            {
                foreach (var filterPair in facetDictionary)
                {
                    searchTasks = searchTasks.Concat(new[]
                    {
                        Task.Run(() =>
                        {
                            var s = facetDictionary.Where(x => x.Key != filterPair.Key && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value);

                            return _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                            {
                                Facets = new[]{ filterPair.Key },
                                SearchMode = (SearchMode)searchModel.SearchMode,
                                IncludeTotalResultCount = true,
                                Filter = string.Join(" and ", facetDictionary.Where(x => x.Key != filterPair.Key && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value)),
                                QueryType = QueryType.Full
                            });
                        })
                    });
                }
            }

            searchTasks = searchTasks.Concat(new[]
            {
                BuildItemsSearchTask(facetDictionary, searchModel)
            });

            return searchTasks;
        }

        private IDictionary<string, string> BuildFacetDictionary(SearchModel searchModel)
        {
            if (searchModel.Filters == null)
                searchModel.Filters = new Dictionary<string, string[]>();

            searchModel.Filters = searchModel.Filters.ToList().Where(m => !m.Value.IsNullOrEmpty())
                .ToDictionary(m => m.Key, m => m.Value);

            IDictionary<string, string> facetDictionary = new Dictionary<string, string>();

            foreach (var facet in Facets)
            {
                string filter = "";
                if (searchModel.Filters.ContainsKey(facet.Name) && searchModel.Filters[facet.Name].AnyWithNullCheck())
                {
                    if (facet.IsMulti)
                        filter = $"({facet.Name}/any(x: {string.Join(" or ", searchModel.Filters[facet.Name].Select(x => $"x eq '{x}'"))}))";
                    else
                        filter = $"({string.Join(" or ", searchModel.Filters[facet.Name].Select(x => $"{facet.Name} eq '{x}'"))})";
                }
                facetDictionary.Add(facet.Name, filter);
            }

            return facetDictionary;
        }

        private async Task<SearchResults<ProvidersIndex>> BuildItemsSearchTask(IDictionary<string, string> facetDictionary, SearchModel searchModel)
        {
            int skip = searchModel.PageNumber > 0 ? (searchModel.PageNumber - 1) * searchModel.Top : 0;

            return await _searchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
            {
                Skip = skip,
                Top = searchModel.Top,
                SearchMode = (SearchMode)searchModel.SearchMode,
                IncludeTotalResultCount = true,
                Filter = string.Join(" and ", facetDictionary.Values.Where(x => !string.IsNullOrWhiteSpace(x))),
                QueryType = QueryType.Full
            }));
        }

        private void ProcessSearchResults(SearchResults<ProvidersIndex> searchResult, ProviderVersionSearchResults results)
        {
            if (!searchResult.Facets.IsNullOrEmpty())
            {
                results.Facets = results.Facets.Concat(searchResult.Facets);
            }
            else
            {
                results.TotalCount = (int)(searchResult?.TotalCount ?? 0);
                results.Results = searchResult?.Results?.Select(m => new ProviderVersionSearchResult
                {
                    Id = m.Result.Id,
                    Name = m.Result.Name,
                    ProviderVersionId = m.Result.ProviderVersionId,
                    ProviderId = m.Result.ProviderId,
                    URN = m.Result.URN,
                    UKPRN = m.Result.UKPRN,
                    UPIN = m.Result.UPIN,
                    EstablishmentNumber = m.Result.EstablishmentNumber,
                    DfeEstablishmentNumber = m.Result.DfeEstablishmentNumber,
                    Authority = m.Result.Authority,
                    ProviderType = m.Result.ProviderType,
                    ProviderSubType = m.Result.ProviderSubType,
                    DateOpened = m.Result.DateOpened,
                    DateClosed = m.Result.DateClosed,
                    ProviderProfileIdType = m.Result.ProviderProfileIdType,
                    LaCode = m.Result.LaCode,
                    NavVendorNo = m.Result.NavVendorNo,
                    CrmAccountId = m.Result.CrmAccountId,
                    LegalName = m.Result.LegalName,
                    Status = m.Result.Status,
                    PhaseOfEducation = m.Result.PhaseOfEducation,
                    ReasonEstablishmentOpened = m.Result.ReasonEstablishmentOpened,
                    ReasonEstablishmentClosed = m.Result.ReasonEstablishmentClosed,
                    Successor = m.Result.Successor,
                    TrustStatus = m.Result.TrustStatus,
                    TrustName = m.Result.TrustName,
                    TrustCode = m.Result.TrustCode,
                    Town = m.Result.Town,
                    Postcode = m.Result.Postcode,
                    LocalAuthorityName = m.Result.LocalAuthorityName,
                    CompaniesHouseNumber = m.Result.CompaniesHouseNumber,
                    GroupIdNumber = m.Result.GroupIdNumber,
                    RscRegionName = m.Result.RscRegionName,
                    RscRegionCode = m.Result.RscRegionCode,
                    GovernmentOfficeRegionName = m.Result.GovernmentOfficeRegionName,
                    GovernmentOfficeRegionCode = m.Result.GovernmentOfficeRegionCode,
                    DistrictCode = m.Result.DistrictCode,
                    DistrictName = m.Result.DistrictName,
                    WardName = m.Result.WardName,
                    WardCode = m.Result.WardCode,
                    CensusWardCode = m.Result.CensusWardCode,
                    CensusWardName = m.Result.CensusWardName,
                    MiddleSuperOutputAreaCode = m.Result.MiddleSuperOutputAreaCode,
                    MiddleSuperOutputAreaName = m.Result.MiddleSuperOutputAreaName,
                    LowerSuperOutputAreaCode = m.Result.LowerSuperOutputAreaCode,
                    LowerSuperOutputAreaName = m.Result.LowerSuperOutputAreaName,
                    ParliamentaryConstituencyCode = m.Result.ParliamentaryConstituencyCode,
                    ParliamentaryConstituencyName = m.Result.ParliamentaryConstituencyName,
                    CountryCode = m.Result.CountryCode,
                    CountryName = m.Result.CountryName
                });
            }
        }
    }
}
