using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
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
        private readonly ISearchRepository<ProviderVersionsIndex> _searchRepository;
        private readonly Policy _providerVersionMetadataRepositoryPolicy;
        private readonly IProviderVersionsMetadataRepository _providerVersionMetadataRepository;
        private readonly IProviderVersionService _providerVersionService;

        public ProviderVersionSearchService(ILogger logger,
            ISearchRepository<ProviderVersionsIndex> searchRepository,
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
            (bool Ok, string Message) searchRepoHealth = await _searchRepository.IsHealthOk();
           
            ServiceHealth providerVersionMetadataRepoHealth = await ((IHealthChecker)_providerVersionMetadataRepository).IsHealthOk();

            ServiceHealth providerVersionServiceHealth = await _providerVersionService.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderVersionSearchService)
            };

            health.Dependencies.AddRange(providerVersionMetadataRepoHealth.Dependencies);
            health.Dependencies.AddRange(providerVersionServiceHealth.Dependencies);

            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });

            return health;
        }

        public async Task<IActionResult> GetProviderById(string providerVersionId, string providerId)
        {
            SearchModel searchModel = new SearchModel { };
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
            SearchResults<ProviderVersionsIndex> searchResults = await BuildItemsSearchTask(searchModel);

            ProviderVersionSearchResults results = new ProviderVersionSearchResults();

            ProcessSearchResults(searchResults, results);

            return results;
        }

        private Task<SearchResults<ProviderVersionsIndex>> BuildItemsSearchTask(SearchModel searchModel)
        {
            return Task.Run(() =>
            {
                return _searchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                {
                    Facets = searchModel.Filters.Select(x => x.Key).ToList(),
                    SearchMode = (SearchMode)searchModel.SearchMode,
                    IncludeTotalResultCount = true,
                    Filter = $"{string.Join(" and ", searchModel.Filters.Select(x => $"{x.Key} eq '{x.Value.First()}'"))}",
                    QueryType = QueryType.Simple
                }));
            });
        }

        private void ProcessSearchResults(SearchResults<ProviderVersionsIndex> searchResult, ProviderVersionSearchResults results)
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
                TrustCode = m.Result.TrustCode
            });
        }
    }
}
