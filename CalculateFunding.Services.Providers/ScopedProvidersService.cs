using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Providers.Caching;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using ProviderSource = CalculateFunding.Common.ApiClient.Models.ProviderSource;

namespace CalculateFunding.Services.Providers
{
    public class ScopedProvidersService : JobProcessingService, IScopedProvidersService, IHealthChecker
    {
        private const string SpecificationId = "specification-id";

        private readonly ICacheProvider _cacheProvider;
        private readonly IResultsApiClient _resultsApiClient;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IProviderVersionService _providerVersionService;
        private readonly IScopedProvidersServiceSettings _scopedProvidersServiceSettings;
        private readonly IFileSystemCache _fileSystemCache;
        private readonly IJobManagement _jobManagement;
        private readonly AsyncPolicy _specificationsPolicy;
        private readonly AsyncPolicy _resultsPolicy;
        private readonly AsyncPolicy _cachePolicy;

        public ScopedProvidersService(ICacheProvider cacheProvider,
            IResultsApiClient resultsApiClient,
            ISpecificationsApiClient specificationsApiClient,
            IProviderVersionService providerVersionService,
            IScopedProvidersServiceSettings scopedProvidersServiceSettings,
            IFileSystemCache fileSystemCache,
            IJobManagement jobManagement,
            IProvidersResiliencePolicies providersResiliencePolicies,
            ILogger logger) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(resultsApiClient, nameof(resultsApiClient));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(providerVersionService, nameof(providerVersionService));
            Guard.ArgumentNotNull(scopedProvidersServiceSettings, nameof(scopedProvidersServiceSettings));
            Guard.ArgumentNotNull(fileSystemCache, nameof(fileSystemCache));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(providersResiliencePolicies?.SpecificationsApiClient, nameof(providersResiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(providersResiliencePolicies?.ResultsApiClient, nameof(providersResiliencePolicies.ResultsApiClient));
            Guard.ArgumentNotNull(providersResiliencePolicies?.CacheProvider, nameof(providersResiliencePolicies.CacheProvider));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _cachePolicy = providersResiliencePolicies.CacheProvider;
            _specificationsPolicy = providersResiliencePolicies.SpecificationsApiClient;
            _resultsPolicy = providersResiliencePolicies.ResultsApiClient;
            _cacheProvider = cacheProvider;
            _resultsApiClient = resultsApiClient;
            _specificationsApiClient = specificationsApiClient;
            _providerVersionService = providerVersionService;
            _fileSystemCache = fileSystemCache;
            _scopedProvidersServiceSettings = scopedProvidersServiceSettings;
            _jobManagement = jobManagement;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth providerVersionServiceHealth = await _providerVersionService.IsHealthOk();
            (bool Ok, string Message) cacheRepoHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(ScopedProvidersService)
            };

            health.Dependencies.AddRange(providerVersionServiceHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth
            {
                HealthOk = cacheRepoHealth.Ok,
                DependencyName = cacheRepoHealth.GetType().GetFriendlyName(),
                Message = cacheRepoHealth.Message
            });

            return health;
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string specificationId = message.GetUserProperty<string>(SpecificationId);

            string scopedProviderSummariesCountCacheKey = $"{CacheKeys.ScopedProviderSummariesCount}{specificationId}";
            string cacheKeyScopedListCacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            SpecificationSummary specificationSummary =  await GetSpecificationSummary(specificationId);

            string providerVersionId = specificationSummary?.ProviderVersionId;
            
            if (string.IsNullOrWhiteSpace(providerVersionId))
            {
                return;
            }

            ProviderVersion providerVersion = await _providerVersionService.GetProvidersByVersion(providerVersionId);

            if (providerVersion == null)
            {
                return;
            }

            IEnumerable<Provider> sourceProviders = providerVersion.Providers;

            if (specificationSummary.ProviderSource == ProviderSource.CFS)
            {
                HashSet<string> scopedProviderIds = (await GetScopedProviderIdsBySpecification(specificationId)).ToHashSet();

                sourceProviders = sourceProviders.Where(_ => scopedProviderIds.Contains(_.ProviderId));
            }

            IEnumerable<ProviderSummary> providerSummaries = CreateProviderSummariesFrom(sourceProviders);

            await _cachePolicy.ExecuteAsync(() => _cacheProvider.KeyDeleteAsync<ProviderSummary>(cacheKeyScopedListCacheKey));

            // Batch to get around redis timeouts
            foreach (IEnumerable<ProviderSummary> batch in providerSummaries.ToBatches(1000))
            {
                // Create list is an upsert into the redis list
                await _cachePolicy.ExecuteAsync(() => _cacheProvider.CreateListAsync(batch, cacheKeyScopedListCacheKey));
                await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetExpiry<ProviderSummary>(cacheKeyScopedListCacheKey, DateTime.UtcNow.AddDays(7)));
            }

            await _cachePolicy.ExecuteAsync(() =>
                _cacheProvider.SetAsync(scopedProviderSummariesCountCacheKey, 
                    providerSummaries.Count().ToString(), TimeSpan.FromDays(7), true));

            string filesystemCacheKey = $"{CacheKeys.ScopedProviderSummariesFilesystemKeyPrefix}{specificationId}";
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.KeyDeleteAsync<string>(filesystemCacheKey));
        }

        public async Task<IActionResult> PopulateProviderSummariesForSpecification(string specificationId,
            string correlationId,
            Reference user,
            bool setCachedProviders)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            return new OkObjectResult(await RegenerateScopedProvidersForSpecification(specificationId, setCachedProviders));
        }

        public async Task<IActionResult> FetchCoreProviderData(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            EnsureFolderExists(ScopedProvidersFileSystemCacheKey.Folder);

            string filesystemCacheKey = $"{CacheKeys.ScopedProviderSummariesFilesystemKeyPrefix}{specificationId}";
            string cacheGuid = await _cachePolicy.ExecuteAsync(() => _cacheProvider.GetAsync<string>(filesystemCacheKey));

            if (cacheGuid.IsNullOrEmpty())
            {
                cacheGuid = Guid.NewGuid().ToString();
                await _cacheProvider.SetAsync(filesystemCacheKey, cacheGuid, TimeSpan.FromDays(7), true);
            }

            ScopedProvidersFileSystemCacheKey cacheKey = new ScopedProvidersFileSystemCacheKey(specificationId, cacheGuid);

            bool isFileSystemCacheEnabled = IsFileSystemCacheEnabled;
            
            if (isFileSystemCacheEnabled && _fileSystemCache.Exists(cacheKey))
            {
                await using Stream cachedStream = _fileSystemCache.Get(cacheKey);

                return GetActionResultForStream(cachedStream, specificationId);
            }

            (bool isFdz, IEnumerable<ProviderSummary> providerSummaries) = await GetScopedProvidersForSpecification(specificationId);

            if (providerSummaries.IsNullOrEmpty())
            {
                return new NoContentResult();
            }

            await using MemoryStream stream = new MemoryStream(providerSummaries.AsJsonBytes())
            {
                Position = 0
            };

            if (isFileSystemCacheEnabled && !isFdz)
            {
                _fileSystemCache.Add(cacheKey, stream);
            }

            return GetActionResultForStream(stream, specificationId);
        }

        private void EnsureFolderExists(string folderName)
        {
            if (!IsFileSystemCacheEnabled)
            {
                return;
            }
            
            _fileSystemCache.EnsureFoldersExist(folderName);
        }

        private bool IsFileSystemCacheEnabled => _scopedProvidersServiceSettings.IsFileSystemCacheEnabled;

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
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            SpecificationSummary specificationSummary = await GetSpecificationSummary(specificationId);

            if (specificationSummary == null)
            {
                throw new ArgumentOutOfRangeException(nameof(specificationId), $"No specification located with Id {specificationId}");
            }

            if (specificationSummary.ProviderSource == ProviderSource.CFS)
            {
                ApiResponse<IEnumerable<string>> scopedProviderIdsRequest =
                    await _resultsPolicy.ExecuteAsync(() => _resultsApiClient.GetScopedProviderIdsBySpecificationId(specificationId));

                if (scopedProviderIdsRequest?.Content == null || scopedProviderIdsRequest.StatusCode != HttpStatusCode.OK)
                {
                    throw new InvalidOperationException("Unable to obtain scoped provider IDs from results service");
                }

                return scopedProviderIdsRequest.Content;
            }
            
            ContentResult coreProviderData = await FetchCoreProviderData(specificationId) as ContentResult;

            string contentLiteral = coreProviderData?.Content;
            
            if (contentLiteral?.IsNullOrWhitespace() == true)
            {
                throw new ArgumentOutOfRangeException(nameof(specificationId), 
                    $"Could not fetch core provider data for specification Id {specificationId}");
            }

            IEnumerable<ProviderSummary> providers = contentLiteral.AsPoco<IEnumerable<ProviderSummary>>();

            return providers.Select(_ => _.Id).ToArray();
        }

        private async Task<(bool isFdz, IEnumerable<ProviderSummary> providers)> GetScopedProvidersForSpecification(string specificationId)
        {
            SpecificationSummary specificationSummary = (await _specificationsPolicy.ExecuteAsync(() => 
                _specificationsApiClient.GetSpecificationSummaryById(specificationId)))?.Content;

            Guard.ArgumentNotNull(specificationSummary, 
                nameof(specificationSummary), $"Did not locate a specification with Id {specificationId}");

            if (specificationSummary.ProviderSource == ProviderSource.CFS)
            {
                return (false, await GetCfsScopedProvidersForSpecification(specificationId));
            }

            return (true, await GetFdzScopedProvidersForSpecification(specificationSummary));
        }

        private async Task<IEnumerable<ProviderSummary>> GetCfsScopedProvidersForSpecification(string specificationId)
        {
            string cacheKeyScopedListCacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";
            long scopedProviderRedisListCount = await _cachePolicy.ExecuteAsync(() => _cacheProvider.ListLengthAsync<ProviderSummary>(cacheKeyScopedListCacheKey));

            return await _cachePolicy.ExecuteAsync(() =>
                _cacheProvider.ListRangeAsync<ProviderSummary>(cacheKeyScopedListCacheKey, 0, (int) scopedProviderRedisListCount));   
        }

        private async Task<IEnumerable<ProviderSummary>> GetFdzScopedProvidersForSpecification(SpecificationSummary specificationSummary)
        {
            string providerVersionId = specificationSummary.ProviderVersionId;
            
            EnsureFolderExists(ProviderVersionFileSystemCacheKey.Folder);

            bool fileSystemEnabled = IsFileSystemCacheEnabled;

            ProviderVersionFileSystemCacheKey fileSystemCacheKey = new ProviderVersionFileSystemCacheKey(providerVersionId);

            if (fileSystemEnabled && _fileSystemCache.Exists(fileSystemCacheKey))
            {
                await using Stream providers = _fileSystemCache.Get(fileSystemCacheKey);

                ProviderVersion cachedProviderVersion = providers.AsPoco<ProviderVersion>();

                return CreateProviderSummariesFrom(cachedProviderVersion.Providers);
            }
            
            //inside the get provider version the file system will be lazily initialised for this provider version id
            ProviderVersion providerVersion = await _providerVersionService.GetProvidersByVersion(providerVersionId);

            Guard.ArgumentNotNull(providerVersion, nameof(providerVersion), $"Did not locate provider version {providerVersionId} in blob storage");

            if (fileSystemEnabled)
            {
                await using Stream providerVersionStream = new MemoryStream(providerVersion.AsJsonBytes());
                
                _fileSystemCache.Add(fileSystemCacheKey, providerVersionStream);
            }

            IEnumerable<Provider> sourceProviders = providerVersion.Providers;

            ProviderSummary[] providerSummaries = CreateProviderSummariesFrom(sourceProviders).ToArray();

            return providerSummaries;
        }

        private static IEnumerable<ProviderSummary> CreateProviderSummariesFrom(IEnumerable<Provider> sourceProviders)
        {
            return sourceProviders
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
                    LocalGovernmentGroupTypeName = x.LocalGovernmentGroupTypeName,
                    Street = x.Street,
                    Locality = x.Locality,
                    Address3 = x.Address3,
                    PaymentOrganisationIdentifier = x.PaymentOrganisationIdentifier,
                    PaymentOrganisationName = x.PaymentOrganisationName,
                    ProviderTypeCode = x.ProviderTypeCode,
                    ProviderSubTypeCode = x.ProviderSubTypeCode,
                    PreviousLAcode = x.PreviousLACode,
                    PreviousLAname = x.PreviousLAName,
                    PreviousEstablishmentNumber = x.PreviousEstablishmentNumber
                });
        }

        private async Task<bool> RegenerateScopedProvidersForSpecification(string specificationId,
            bool setCachedProviders)
        {
            string scopedProviderSummariesCountCacheKey = $"{CacheKeys.ScopedProviderSummariesCount}{specificationId}";
            string currentProviderCount = await _cachePolicy.ExecuteAsync(() => _cacheProvider.GetAsync<string>(scopedProviderSummariesCountCacheKey));

            string cacheKeyScopedListCacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";
            long scopedProviderRedisListCount = await _cachePolicy.ExecuteAsync(() => _cacheProvider.ListLengthAsync<ProviderSummary>(cacheKeyScopedListCacheKey));

            if (string.IsNullOrWhiteSpace(currentProviderCount) || int.Parse(currentProviderCount) != scopedProviderRedisListCount || setCachedProviders)
            {
                IEnumerable<JobSummary> latestJob = await _jobManagement.GetLatestJobsForSpecification(specificationId,
                    new[]
                    {
                        JobConstants.DefinitionNames.PopulateScopedProvidersJob
                    });

                // the populate scoped providers job is already running so don't need to queue another job
                if (latestJob?.FirstOrDefault()?.RunningStatus == RunningStatus.InProgress)
                {
                    return true;
                }

                await _jobManagement.QueueJob(new JobCreateModel
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
                        {
                            "specification-id", specificationId
                        }
                    }
                });

                return true;
            }

            return false;
        }

        private IActionResult GetActionResultForStream(Stream stream,
            string specificationId)
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
                    StatusCode = (int) HttpStatusCode.OK
                };
            }

            return new NoContentResult();
        }

        private async Task<SpecificationSummary> GetSpecificationSummary(string specificationId)
            => (await _specificationsPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId)))?.Content;
    }
}