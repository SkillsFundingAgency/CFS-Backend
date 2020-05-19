using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Models.Providers;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Providers.Caching;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Providers
{
    public class ScopedProvidersService : IScopedProvidersService, IHealthChecker
    {
        private const string getSpecificationSummary = "specs/specification-summary-by-id?specificationId={0}";
        private const string getProviderVersion = "providers/versions/{0}";
        private const string getScopedProviderIdsUrl = "results/get-scoped-providerids?specificationId=";

        private readonly ICacheProvider _cacheProvider;
        private readonly IResultsApiClient _resultsApiClient;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IProviderVersionService _providerVersionService;
        private readonly IMapper _mapper;
        private readonly IScopedProvidersServiceSettings _scopedProvidersServiceSettings;
        private readonly IFileSystemCache _fileSystemCache;
        private readonly IJobsApiClient _jobsApiClient;
        private readonly IJobManagement _jobManagement;
        private readonly AsyncPolicy _jobApiClientPolicy;
        private readonly ILogger _logger;
        private const string SpecificationId = "specification-id";
        private const string JobId = "jobId";

        public ScopedProvidersService(ICacheProvider cacheProvider,
            IResultsApiClient resultsApiClient,
            IJobsApiClient jobsApiClient,
            ISpecificationsApiClient specificationsApiClient,
            IProviderVersionService providerVersionService,
            IMapper mapper,
            IScopedProvidersServiceSettings scopedProvidersServiceSettings,
            IFileSystemCache fileSystemCache,
            IJobManagement jobManagement,
            IProvidersResiliencePolicies providersResiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(resultsApiClient, nameof(resultsApiClient));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(providerVersionService, nameof(providerVersionService));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(scopedProvidersServiceSettings, nameof(scopedProvidersServiceSettings));
            Guard.ArgumentNotNull(fileSystemCache, nameof(fileSystemCache));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(providersResiliencePolicies, nameof(providersResiliencePolicies));
            Guard.ArgumentNotNull(providersResiliencePolicies.JobsApiClient, nameof(providersResiliencePolicies.JobsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _cacheProvider = cacheProvider;
            _resultsApiClient = resultsApiClient;
            _specificationsApiClient = specificationsApiClient;
            _providerVersionService = providerVersionService;
            _mapper = mapper;
            _fileSystemCache = fileSystemCache;
            _scopedProvidersServiceSettings = scopedProvidersServiceSettings;
            _jobsApiClient = jobsApiClient;
            _jobManagement = jobManagement;
            _jobApiClientPolicy = providersResiliencePolicies.JobsApiClient;
            _logger = logger;
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

        public async Task PopulateScopedProviders(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string jobId = message.GetUserProperty<string>(JobId);

            string specificationId = message.GetUserProperty<string>(SpecificationId);

            string scopedProviderSummariesCountCacheKey = $"{CacheKeys.ScopedProviderSummariesCount}{specificationId}";
            string cacheKeyScopedListCacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";
            
            await _jobManagement.UpdateJobStatus(jobId, 0, null);

            ApiResponse<SpecificationSummary> spec =
                 await _specificationsApiClient.GetSpecificationSummaryById(specificationId);

            if (string.IsNullOrWhiteSpace(spec?.Content.ProviderVersionId))
            {
                return;
            }

            ProviderVersion providerVersion = await _providerVersionService.GetProvidersByVersion(spec.Content.ProviderVersionId);

            if (providerVersion == null)
            {
                return;
            }

            IEnumerable<string> scopedProviderIds = await GetScopedProviderIdsBySpecification(specificationId);

            IEnumerable<ProviderSummary> providerSummaries = providerVersion.Providers.Where(p => scopedProviderIds.Contains(p.ProviderId))
                .Select(x => new ProviderSummary
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

            await _cacheProvider.KeyDeleteAsync<ProviderSummary>(cacheKeyScopedListCacheKey);

            // Batch to get around redis timeouts
            foreach (IEnumerable<ProviderSummary> batch in providerSummaries.ToBatches(1000))
            {
                // Create list is an upsert into the redis list
                await _cacheProvider.CreateListAsync(batch, cacheKeyScopedListCacheKey);
                await _cacheProvider.SetExpiry<ProviderSummary>(cacheKeyScopedListCacheKey, DateTime.UtcNow.AddDays(7));
            }

            await _cacheProvider.SetAsync(scopedProviderSummariesCountCacheKey, providerSummaries.Count().ToString(), TimeSpan.FromDays(7), true);

            string filesystemCacheKey = $"{CacheKeys.ScopedProviderSummariesFilesystemKeyPrefix}{specificationId}";
            await _cacheProvider.KeyDeleteAsync<string>(filesystemCacheKey);

            // Mark job as complete
            _logger.Information($"Marking populate scoped providers job complete");

            await _jobManagement.UpdateJobStatus(jobId, 0, 0, true, null);
            _logger.Information($"Populate scoped providers job complete");
        }

        public async Task<IActionResult> PopulateProviderSummariesForSpecification(string specificationId, string correlationId, Reference user, bool setCachedProviders)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            
            return new OkObjectResult(await RegenerateScopedProvidersForSpecification(specificationId, setCachedProviders));
        }

        public async Task<IActionResult> FetchCoreProviderData(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            bool fileSystemCacheEnabled = _scopedProvidersServiceSettings.IsFileSystemCacheEnabled;

            if (fileSystemCacheEnabled)
            {
                _fileSystemCache.EnsureFoldersExist(ScopedProvidersFileSystemCacheKey.Folder);
            }

            string filesystemCacheKey = $"{CacheKeys.ScopedProviderSummariesFilesystemKeyPrefix}{specificationId}";
            string cacheGuid = await _cacheProvider.GetAsync<string>(filesystemCacheKey);

            if (cacheGuid.IsNullOrEmpty())
            {
                cacheGuid = Guid.NewGuid().ToString();
                await _cacheProvider.SetAsync(filesystemCacheKey, cacheGuid, TimeSpan.FromDays(7), true);
            }

            ScopedProvidersFileSystemCacheKey cacheKey = new ScopedProvidersFileSystemCacheKey(specificationId, cacheGuid);

            if (fileSystemCacheEnabled && _fileSystemCache.Exists(cacheKey))
            {
                using Stream cachedStream = _fileSystemCache.Get(cacheKey);
                return GetActionResultForStream(cachedStream, specificationId);
            }

            IEnumerable<ProviderSummary> providerSummaries = await this.GetScopedProvidersForSpecification(specificationId);
            if (providerSummaries.IsNullOrEmpty())
            {
                return new NoContentResult();
            }

            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(providerSummaries)))
            {
                Position = 0
            };

            if (fileSystemCacheEnabled)
            {
                _fileSystemCache.Add(cacheKey, stream);
            }

            return GetActionResultForStream(stream, specificationId);
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

        private async Task<IEnumerable<string>> GetScopedProviderIdsBySpecification(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                throw new ArgumentNullException(nameof(specificationId));
            }

            Common.ApiClient.Models.ApiResponse<IEnumerable<string>> scopedProviderIdsRequest =
                    await _resultsApiClient.GetScopedProviderIdsBySpecificationId(specificationId);

            if (scopedProviderIdsRequest == null || scopedProviderIdsRequest.Content == null || scopedProviderIdsRequest.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException("Unable to obtain scoped provider IDs from results service");
            }

            return scopedProviderIdsRequest.Content;

        }

        private async Task<IEnumerable<ProviderSummary>> GetScopedProvidersForSpecification(string specificationId)
        {
            string cacheKeyScopedListCacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";
            long scopedProviderRedisListCount = await _cacheProvider.ListLengthAsync<ProviderSummary>(cacheKeyScopedListCacheKey);

            return await _cacheProvider.ListRangeAsync<ProviderSummary>(cacheKeyScopedListCacheKey, 0, (int)scopedProviderRedisListCount);
        }

        private async Task<bool> RegenerateScopedProvidersForSpecification(string specificationId, bool setCachedProviders)
        {
            string scopedProviderSummariesCountCacheKey = $"{CacheKeys.ScopedProviderSummariesCount}{specificationId}";
            string currentProviderCount = await _cacheProvider.GetAsync<string>(scopedProviderSummariesCountCacheKey);

            string cacheKeyScopedListCacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";
            long scopedProviderRedisListCount = await _cacheProvider.ListLengthAsync<ProviderSummary>(cacheKeyScopedListCacheKey);

            if (string.IsNullOrWhiteSpace(currentProviderCount) || int.Parse(currentProviderCount) != scopedProviderRedisListCount || setCachedProviders)
            {
                ApiResponse<JobSummary> latestJob = await _jobApiClientPolicy.ExecuteAsync(() => _jobsApiClient.GetLatestJobForSpecification(specificationId, new[] { JobConstants.DefinitionNames.PopulateScopedProvidersJob }));

                // the populate scoped providers job is already running so don't need to queue another job
                if(latestJob?.Content != null && latestJob?.Content.RunningStatus == RunningStatus.InProgress)
                {
                    return true;
                }

                await _jobApiClientPolicy.ExecuteAsync(() => _jobsApiClient.CreateJob(new JobCreateModel
                {
                    JobDefinitionId = JobConstants.DefinitionNames.PopulateScopedProvidersJob,
                    SpecificationId = specificationId,
                    Trigger = new Trigger
                    {
                        EntityId = specificationId,
                        EntityType = "Specification",
                        Message = "Triggered for specification changes"
                    },
                    Properties = new Dictionary<string, string>
                {
                    {"specification-id", specificationId}
                }
                }));

                return true;
            }
            else
            {
                return false;
            }
        }

        private IActionResult GetActionResultForStream(Stream stream, string specificationId)
        {
            if (stream == null || stream.Length == 0)
            {
                return new PreconditionFailedResult($"Blob for specificationId: {specificationId}  not found");
            }

            stream.Position = 0;

            using StreamReader reader = new StreamReader(stream);
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
