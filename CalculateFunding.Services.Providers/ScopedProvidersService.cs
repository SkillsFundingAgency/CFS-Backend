using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Providers
{
    public class ScopedProvidersService : IScopedProvidersService, IHealthChecker
    {
        private const string getSpecificationSummary = "specs/specification-summary-by-id?specificationId={0}";
        private const string getProviderVersion = "providers/versions/{0}";
        private const string getScopedProviderIdsUrl = "results/get-scoped-providerids?specificationId=";

        private readonly ICacheProvider _cacheProvider;      
        private readonly IResultsApiClientProxy _resultsApiClient;
        private readonly ISpecificationsApiClientProxy _specificationsApiClient;
        private readonly IProviderVersionService _providerVersionService;
        private readonly IMapper _mapper;

        public ScopedProvidersService(ICacheProvider cacheProvider, IResultsApiClientProxy resultsApiClient, ISpecificationsApiClientProxy specificationsApiClient, IProviderVersionService providerVersionService, IMapper mapper)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(resultsApiClient, nameof(resultsApiClient));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(providerVersionService, nameof(providerVersionService));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _cacheProvider = cacheProvider;
            _resultsApiClient = resultsApiClient;
            _specificationsApiClient = specificationsApiClient;
            _providerVersionService = providerVersionService;
            _mapper = mapper;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth providerVersionServiceHealth = await _providerVersionService.IsHealthOk();
            (bool Ok, string Message) cacheRepoHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ScopedProvidersService)
            };

            health.Dependencies.AddRange(providerVersionServiceHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheRepoHealth.Ok, DependencyName = cacheRepoHealth.GetType().GetFriendlyName(), Message = cacheRepoHealth.Message });

            return health;
        }

        public async Task<IActionResult> PopulateProviderSummariesForSpecification(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            string cacheKeyScopedProviderSummariesCount = $"{CacheKeys.ScopedProviderSummariesCount}{specificationId}";
            string cacheKeyAllProviderSummaries = $"{CacheKeys.AllProviderSummaries}{specificationId}";
            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            IEnumerable<ProviderSummary> allCachedProviders = Enumerable.Empty<ProviderSummary>();

            string providerCount = await _cacheProvider.GetAsync<string>(cacheKeyScopedProviderSummariesCount);
            int allSummariesCount = 0;

            if (!string.IsNullOrWhiteSpace(providerCount) && !int.TryParse(providerCount, out allSummariesCount))
            {
                throw new Exception("Invalid provider count in cache");
            }

            if (allSummariesCount > 0)
            {
                allCachedProviders = await _cacheProvider.ListRangeAsync<ProviderSummary>(cacheKeyAllProviderSummaries, 0, allSummariesCount);
            }

            if (allSummariesCount < 1 || allCachedProviders.IsNullOrEmpty())
            {
                allCachedProviders = await GetScopedProvidersBySpecification(specificationId);
                allSummariesCount = allCachedProviders.Count();
            }

            if (allSummariesCount < 1 || allCachedProviders.IsNullOrEmpty())
            {
                throw new InvalidOperationException($"No provider summaries exist in cache or provider versions");
            }

            IEnumerable<string> providerIds = await GetScopedProviderIdsBySpecification(specificationId);

            int providerIdCount = providerIds.Count();

            IList<ProviderSummary> providerSummaries = new List<ProviderSummary>(providerIdCount);

            foreach (string providerId in providerIds)
            {
                ProviderSummary cachedProvider = allCachedProviders.FirstOrDefault(m => m.Id == providerId);

                if (cachedProvider != null)
                {
                    providerSummaries.Add(cachedProvider);
                }
            }

            await _cacheProvider.KeyDeleteAsync<ProviderSummary>(cacheKey);

            await _cacheProvider.CreateListAsync(providerSummaries, cacheKey);
            await _cacheProvider.SetExpiry<ProviderSummary>(cacheKey, DateTime.UtcNow.AddDays(7));

            return new OkObjectResult(providerSummaries.Count());
        }

        public async Task<IActionResult> FetchCoreProviderData(string specificationId)
        {
            IEnumerable<ProviderSummary> providerSummaries = await this.GetScopedProvidersBySpecification(specificationId);

            if (providerSummaries.IsNullOrEmpty())
            {
                return new NoContentResult();
            }

            return new OkObjectResult(providerSummaries);
        }

        public async Task<IActionResult> GetScopedProviderIds(string specificationId)
        {
            IEnumerable<string> providerIds = await GetScopedProviderIdsBySpecification(specificationId);

            if (providerIds.IsNullOrEmpty())
            {
                return new NoContentResult();
            }

            return new OkObjectResult(providerIds);
        }

        private Task<IEnumerable<string>> GetScopedProviderIdsBySpecification(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                throw new ArgumentNullException(nameof(specificationId));
            }

            string url = $"{getScopedProviderIdsUrl}{specificationId}";

            return _resultsApiClient.GetAsync<IEnumerable<string>>(url);
        }

        private async Task<IEnumerable<ProviderSummary>> GetScopedProvidersBySpecification(string specificationId)
        {
            string url = string.Format(getSpecificationSummary, specificationId);

            SpecificationSummary spec = await _specificationsApiClient.GetAsync<SpecificationSummary>(url);

            if (string.IsNullOrWhiteSpace(spec?.ProviderVersionId))
            {
                return null;
            }

            ProviderVersion providerVersion = await _providerVersionService.GetProvidersByVersion(spec.ProviderVersionId);

            if (providerVersion == null)
            {
                return null;
            }

            int totalCount = providerVersion.Providers.Count();

            string cacheKeyScopedProviderSummariesCount = $"{CacheKeys.ScopedProviderSummariesCount}{specificationId}";
            string currentProviderCount = await _cacheProvider.GetAsync<string>(cacheKeyScopedProviderSummariesCount);

            string cacheKeyAllProviderSummaries = $"{CacheKeys.AllProviderSummaries}{specificationId}";
            long totalProviderListCount = await _cacheProvider.ListLengthAsync<ProviderSummary>(cacheKeyAllProviderSummaries);

            if (string.IsNullOrWhiteSpace(currentProviderCount) || int.Parse(currentProviderCount) != totalCount || totalProviderListCount != totalCount)
            {
                IEnumerable<ProviderSummary> providerSummaries = providerVersion.Providers.Select(x => new ProviderSummary
                {
                    Name = x.Name,
                    Id = x.ProviderId,
                    ProviderProfileIdType = x.ProviderProfileIdType,
                    UKPRN = x.UKPRN,
                    URN = x.URN,
                    Authority = x.Authority,
                    UPIN = x.UPIN,
                    ProviderSubType = x.ProviderSubType,
                    EstablishmentNumber = x.EstablishmentNumber,
                    ProviderType = x.ProviderType,
                    DateOpened = x.DateOpened,
                    DateClosed = x.DateClosed,
                    LACode = x.LACode,
                    CrmAccountId = x.CrmAccountId,
                    LegalName = x.LegalName,
                    NavVendorNo = x.NavVendorNo,
                    DfeEstablishmentNumber = x.DfeEstablishmentNumber,
                    Status = x.Status,
                    PhaseOfEducation = x.PhaseOfEducation,
                    ReasonEstablishmentClosed = x.ReasonEstablishmentClosed,
                    ReasonEstablishmentOpened = x.ReasonEstablishmentOpened,
                    Successor = x.Successor,
                    TrustStatus = x.TrustStatus,
                    TrustName = x.TrustName,
                    TrustCode = x.TrustCode,
                    Town = x.Town,
                    Postcode = x.Postcode,
                    LocalAuthorityName = x.LocalAuthorityName,
                    CompaniesHouseNumber = x.CompaniesHouseNumber,
                    GroupIdNumber = x.GroupIdNumber,
                    RscRegionName = x.RscRegionName,
                    RscRegionCode = x.RscRegionCode,
                    GovernmentOfficeRegionName = x.GovernmentOfficeRegionName,
                    GovernmentOfficeRegionCode = x.GovernmentOfficeRegionCode,
                    DistrictCode = x.DistrictCode,
                    DistrictName = x.DistrictName,
                    WardName = x.WardName,
                    WardCode = x.WardCode,
                    CensusWardCode = x.CensusWardCode,
                    CensusWardName = x.CensusWardName,
                    MiddleSuperOutputAreaCode = x.MiddleSuperOutputAreaCode,
                    MiddleSuperOutputAreaName = x.MiddleSuperOutputAreaName,
                    LowerSuperOutputAreaCode = x.LowerSuperOutputAreaCode,
                    LowerSuperOutputAreaName = x.LowerSuperOutputAreaName,
                    ParliamentaryConstituencyCode = x.ParliamentaryConstituencyCode,
                    ParliamentaryConstituencyName = x.ParliamentaryConstituencyName,
                    CountryCode = x.CountryCode,
                    CountryName = x.CountryName,
                    LocalGovernmentGroupTypeCode = x.LocalGovernmentGroupTypeCode,
                    LocalGovernmentGroupTypeName = x.LocalGovernmentGroupTypeName
                });

                await _cacheProvider.KeyDeleteAsync<ProviderSummary>(cacheKeyAllProviderSummaries);

                // Batch to get around redis timeouts
                foreach (IEnumerable<ProviderSummary> batch in providerSummaries.ToBatches(1000))
                {
                    await _cacheProvider.CreateListAsync(batch, cacheKeyAllProviderSummaries);
                    await _cacheProvider.SetExpiry<ProviderSummary>(cacheKeyAllProviderSummaries, DateTime.UtcNow.AddDays(7));
                }

                await _cacheProvider.SetAsync(cacheKeyScopedProviderSummariesCount, totalCount.ToString(), TimeSpan.FromDays(7), true);
                return providerSummaries;
            }
            else
            {
                return await _cacheProvider.ListRangeAsync<ProviderSummary>(cacheKeyAllProviderSummaries, 0, totalCount);
            }
        }
    }
}
