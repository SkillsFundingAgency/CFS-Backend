using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Providers.Caching;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using Serilog;

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
        private readonly IScopedProvidersServiceSettings _scopedProvidersServiceSettings;
        private static volatile bool _haveCheckedFileSystemCacheFolder;
        private readonly IFileSystemCache _fileSystemCache;
             

        public ScopedProvidersService(ICacheProvider cacheProvider, 
            IResultsApiClientProxy resultsApiClient, 
            ISpecificationsApiClientProxy specificationsApiClient, 
            IProviderVersionService providerVersionService, 
            IMapper mapper,
            IScopedProvidersServiceSettings scopedProvidersServiceSettings,
            IFileSystemCache fileSystemCache)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(resultsApiClient, nameof(resultsApiClient));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(providerVersionService, nameof(providerVersionService));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(scopedProvidersServiceSettings, nameof(scopedProvidersServiceSettings));
            Guard.ArgumentNotNull(fileSystemCache, nameof(fileSystemCache));
                      

            _cacheProvider = cacheProvider;
            _resultsApiClient = resultsApiClient;
            _specificationsApiClient = specificationsApiClient;
            _providerVersionService = providerVersionService;
            _mapper = mapper;
            _fileSystemCache = fileSystemCache;
            _scopedProvidersServiceSettings = scopedProvidersServiceSettings;                    
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
            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}-CurrentVersion";
            await _cacheProvider.SetAsync(cacheKey, Guid.NewGuid().ToString(), TimeSpan.FromDays(7),true);

            await _cacheProvider.KeyDeleteAsync<ProviderSummary>(cacheKey);

            await _cacheProvider.CreateListAsync(providerSummaries, cacheKey);
            await _cacheProvider.SetExpiry<ProviderSummary>(cacheKey, DateTime.UtcNow.AddDays(7));

            return new OkObjectResult(providerSummaries.Count());
        }

        public async Task<IActionResult> FetchCoreProviderData(string specificationId)
        {           

            IEnumerable<ProviderSummary> scopedProviderSummaries = await GetScopedProviders(specificationId);
            if (scopedProviderSummaries.IsNullOrEmpty())
            {
                return new NoContentResult();
            }

            return new OkObjectResult(scopedProviderSummaries);
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

        private async Task<IEnumerable<ProviderSummary>> GetScopedProviders(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            
            ContentResult contentResult = (ContentResult)await GetAllScopedProviders(specificationId);

            return JsonConvert.DeserializeObject<IEnumerable<ProviderSummary>>(contentResult.Content);
        }

        private async Task<IActionResult> GetAllScopedProviders(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            bool fileSystemCacheEnabled = _scopedProvidersServiceSettings.IsFileSystemCacheEnabled;

            if (fileSystemCacheEnabled && !_haveCheckedFileSystemCacheFolder)
            {               
               _fileSystemCache.EnsureFoldersExist(ScopedProvidersFileSystemCacheKey.Folder);               
            }
           
            string redisCacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}-CurrentVersion";
            string cacheGuid = await _cacheProvider.GetAsync<string>(redisCacheKey);
           
            if(cacheGuid.IsNullOrEmpty())
            {
                cacheGuid = Guid.NewGuid().ToString();
                await _cacheProvider.SetAsync(redisCacheKey, cacheGuid, TimeSpan.FromDays(7), true);
            }
           
            ScopedProvidersFileSystemCacheKey cacheKey = new ScopedProvidersFileSystemCacheKey(specificationId, cacheGuid);


            if (fileSystemCacheEnabled && _fileSystemCache.Exists(cacheKey))
            {
                using (Stream cachedStream = _fileSystemCache.Get(cacheKey))
                {
                    return GetActionResultForStream(cachedStream, specificationId);
                }
            }
            
            IEnumerable<ProviderSummary> providerSummaries = await this.GetScopedProvidersBySpecification(specificationId);
            if (providerSummaries.IsNullOrEmpty())
            {
                return new NoContentResult();
            }
            
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(providerSummaries))))
            { 
                stream.Position = 0;

                if (fileSystemCacheEnabled)
                {
                    _fileSystemCache.Add(cacheKey, stream);
                }

                return GetActionResultForStream(stream, specificationId);
            }
        }

        private IActionResult GetActionResultForStream(Stream stream, string specificationId)
        {
            if (stream == null || stream.Length == 0)
            {               
                return new PreconditionFailedResult($"Blob for specificationId: {specificationId}  not found");
            }

            stream.Position = 0;

            using (StreamReader reader = new StreamReader(stream))
            {
                string providerVersionString = reader.ReadToEnd();

                if (!string.IsNullOrWhiteSpace(providerVersionString))
                {
                    return new ContentResult
                    {
                        Content = providerVersionString,
                        ContentType = "application/json",
                        StatusCode = (int)HttpStatusCode.OK
                    };
                }

                return new NoContentResult();
            }
        }
    }
}
