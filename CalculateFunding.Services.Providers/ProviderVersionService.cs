using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Providers.Caching;
using CalculateFunding.Services.Providers.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Providers
{
    public class ProviderVersionService : IProviderVersionService, IHealthChecker
    {
        private const int CACHE_DURATION = 7;

        private readonly ICacheProvider _cacheProvider;
        private readonly IBlobClient _blobClient;
        private readonly ILogger _logger;
        private readonly IValidator<ProviderVersionViewModel> _providerVersionModelValidator;
        private readonly IProviderVersionsMetadataRepository _providerVersionMetadataRepository;
        private readonly Policy _providerVersionMetadataRepositoryPolicy;
        private readonly Policy _blobRepositoryPolicy;
        private readonly IMapper _mapper;
        private readonly IFileSystemCache _fileSystemCache;
        private readonly IProviderVersionServiceSettings _providerVersionServiceSettings;
        private static volatile bool _haveCheckedFileSystemCacheFolder;
        

        public ProviderVersionService(ICacheProvider cacheProvider,
            IBlobClient blobClient,
            ILogger logger,
            IValidator<ProviderVersionViewModel> providerVersionModelValidator,
            IProviderVersionsMetadataRepository providerVersionMetadataRepository,
            IProvidersResiliencePolicies resiliencePolicies,
            IMapper mapper,
            IFileSystemCache fileSystemCache,
            IProviderVersionServiceSettings providerVersionServiceSettings)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(providerVersionModelValidator, nameof(providerVersionModelValidator));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(fileSystemCache, nameof(fileSystemCache));
            Guard.ArgumentNotNull(providerVersionServiceSettings, nameof(providerVersionServiceSettings));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));

            _cacheProvider = cacheProvider;
            _blobClient = blobClient;
            _logger = logger;
            _providerVersionModelValidator = providerVersionModelValidator;
            _providerVersionMetadataRepository = providerVersionMetadataRepository;
            _providerVersionMetadataRepositoryPolicy = resiliencePolicies.ProviderVersionMetadataRepository;
            _blobRepositoryPolicy = resiliencePolicies.BlobRepositoryPolicy;

            _mapper = mapper;
            _fileSystemCache = fileSystemCache;
            _providerVersionServiceSettings = providerVersionServiceSettings;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth providerVersionMetadataRepoHealth = await ((IHealthChecker) _providerVersionMetadataRepository).IsHealthOk();
            (bool Ok, string Message) blobClientRepoHealth = await _blobClient.IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(ProviderVersionService)
            };
            health.Dependencies.AddRange(providerVersionMetadataRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth
            {
                HealthOk = blobClientRepoHealth.Ok,
                DependencyName = blobClientRepoHealth.GetType().GetFriendlyName(), Message = blobClientRepoHealth.Message
            });

            return health;
        }

        public async Task<IActionResult> DoesProviderVersionExist(string providerVersionId)
        {
            Guard.ArgumentNotNull(providerVersionId, nameof(providerVersionId));

            if (await Exists(providerVersionId))
            {
                return new NoContentResult();
            }

            return new NotFoundResult();
        }

        public async Task<IActionResult> GetAllProviders(int year, int month, int day)
        {
            Guard.ArgumentNotNull(day, nameof(day));
            Guard.ArgumentNotNull(month, nameof(month));
            Guard.ArgumentNotNull(year, nameof(year));

            ProviderVersionByDate providerVersionByDate = await GetProviderVersionByDate(year, month, day);

            if (providerVersionByDate != null)
            {
                return await GetAllProviders(providerVersionByDate.ProviderVersionId);
            }

            return new NotFoundResult();
        }

        public async Task<ProviderVersionByDate> GetProviderVersionByDate(int year, int month, int day)
        {
            string localCacheKey = $"{CacheKeys.ProviderVersionByDate}{year}{month:00}{day:00}";
            ProviderVersionByDate providerVersionByDate = await _cacheProvider.GetAsync<ProviderVersionByDate>(localCacheKey);

            if (providerVersionByDate == null)
            {
                providerVersionByDate = await _providerVersionMetadataRepositoryPolicy.ExecuteAsync(() => _providerVersionMetadataRepository.GetProviderVersionByDate(year, month, day));
                await _cacheProvider.SetAsync(localCacheKey, providerVersionByDate, TimeSpan.FromDays(CACHE_DURATION), true);
            }

            return providerVersionByDate;
        }

        public async Task<IActionResult> GetProviderVersionsByFundingStream(string fundingStream)
        {
            IEnumerable<ProviderVersionMetadata> providerVersions =
                await _providerVersionMetadataRepositoryPolicy.ExecuteAsync(() => _providerVersionMetadataRepository.GetProviderVersions(fundingStream));

            if (providerVersions != null)
            {
                return new OkObjectResult(providerVersions);
            }

            return new NotFoundResult();
        }

        public async Task<MasterProviderVersion> GetMasterProviderVersion()
        {
            MasterProviderVersion masterProviderVersion = await _cacheProvider.GetAsync<MasterProviderVersion>(CacheKeys.MasterProviderVersion);

            if (masterProviderVersion == null)
            {
                masterProviderVersion = await _providerVersionMetadataRepositoryPolicy.ExecuteAsync(() => _providerVersionMetadataRepository.GetMasterProviderVersion());
                await _cacheProvider.SetAsync(CacheKeys.MasterProviderVersion, masterProviderVersion, TimeSpan.FromDays(CACHE_DURATION), true);
            }

            return masterProviderVersion;
        }

        public async Task<IActionResult> GetAllMasterProviders()
        {
            MasterProviderVersion masterProviderVersion = await GetMasterProviderVersion();

            if (masterProviderVersion != null)
            {
                return await GetAllProviders(masterProviderVersion.ProviderVersionId);
            }

            return new NotFoundResult();
        }

        public async Task<ProviderVersion> GetProvidersByVersion(string providerVersionId)
        {
            Guard.IsNullOrWhiteSpace(providerVersionId, nameof(providerVersionId));

            OkObjectResult okObjectResult = await GetAllProviders(providerVersionId) as OkObjectResult;

            return okObjectResult.Value as ProviderVersion;
        }

        public async Task<IActionResult> GetAllProviders(string providerVersionId)
        {
            Guard.IsNullOrWhiteSpace(providerVersionId, nameof(providerVersionId));

            string blobName = $"{providerVersionId}.json";
            
            bool fileSystemCacheEnabled = _providerVersionServiceSettings.IsFileSystemCacheEnabled;

            if (fileSystemCacheEnabled && !_haveCheckedFileSystemCacheFolder)
            {
                _fileSystemCache.EnsureFoldersExist(ProviderVersionFileSystemCacheKey.Folder);
            }
            
            ProviderVersionFileSystemCacheKey cacheKey = new ProviderVersionFileSystemCacheKey(providerVersionId);
         
            
            if (fileSystemCacheEnabled && _fileSystemCache.Exists(cacheKey))
            {
                using (Stream cachedStream = _fileSystemCache.Get(cacheKey))
                {
                    return GetActionResultForStream(cachedStream, providerVersionId);
                }
            }

            ICloudBlob blob = _blobClient.GetBlockBlobReference(blobName);

            if (!blob.Exists())
            {
                _logger.Error($"Failed to find blob with path: {blobName}");
                
                return new NotFoundResult();
                
            }

            using (Stream blobClientStream = await _blobClient.DownloadToStreamAsync(blob))
            {
                if (fileSystemCacheEnabled)
                {
                    _fileSystemCache.Add(cacheKey, blobClientStream);
                }
                
                return GetActionResultForStream(blobClientStream, providerVersionId);
            }
        }

        private IActionResult GetActionResultForStream(Stream stream, string providerVersionId)
        {
            if (stream == null || stream.Length == 0)
            {
                _logger.Error($"Blob for provider version id: {providerVersionId} not found");
                return new PreconditionFailedResult($"Blob for provider version id: {providerVersionId}  not found");
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

        public async Task<bool> Exists(string providerVersionId)
        {
            Guard.ArgumentNotNull(providerVersionId, nameof(providerVersionId));

            string blobName = providerVersionId + ".json";

            return await _blobClient.BlobExistsAsync(blobName);
        }

        public async Task<bool> Exists(ProviderVersionViewModel providerVersionModel)
        {
            Guard.ArgumentNotNull(providerVersionModel, nameof(providerVersionModel));

            return await _providerVersionMetadataRepository.Exists(providerVersionModel.Name,
                providerVersionModel.ProviderVersionTypeString,
                providerVersionModel.Version,
                providerVersionModel.FundingStream);
        }

        public async Task<IActionResult> SetProviderVersionByDate(int year, int month, int day, string providerVersionId)
        {
            Guard.ArgumentNotNull(year, nameof(year));
            Guard.ArgumentNotNull(month, nameof(month));
            Guard.ArgumentNotNull(day, nameof(day));
            Guard.IsNullOrWhiteSpace(providerVersionId, nameof(providerVersionId));

            bool exists = await Exists(providerVersionId);

            if (exists)
            {
                ProviderVersionByDate newProviderVersionByDate = new ProviderVersionByDate {ProviderVersionId = providerVersionId, Day = day, Month = month, Year = year};

                string localCacheKey = $"{CacheKeys.ProviderVersionByDate}{year}{month:00}{day:00}";

                ProviderVersionByDate providerVersionByDate = await _cacheProvider.GetAsync<ProviderVersionByDate>(localCacheKey);

                if (providerVersionByDate != null)
                {
                    await _cacheProvider.RemoveAsync<ProviderVersionByDate>(localCacheKey);
                }

                await _providerVersionMetadataRepositoryPolicy.ExecuteAsync(() => _providerVersionMetadataRepository.UpsertProviderVersionByDate(newProviderVersionByDate));
            }
            else
            {
                _logger.Error($"Failed to retrieve provider version with id: {providerVersionId}");
            }

            return new NoContentResult();
        }

        public async Task<IActionResult> SetMasterProviderVersion(MasterProviderVersionViewModel masterProviderVersionViewModel)
        {
            Guard.ArgumentNotNull(masterProviderVersionViewModel, nameof(masterProviderVersionViewModel));

            MasterProviderVersion newMasterProviderVersion = _mapper.Map<MasterProviderVersion>(masterProviderVersionViewModel);

            bool exists = await Exists(newMasterProviderVersion.ProviderVersionId);

            if (!exists)
            {
                string error = $"Failed to retrieve provider version with id: {newMasterProviderVersion.ProviderVersionId}";
                _logger.Error(error);
                return new PreconditionFailedResult(error);
            }

            MasterProviderVersion masterProviderVersion = await _cacheProvider.GetAsync<MasterProviderVersion>(CacheKeys.MasterProviderVersion);

            if (masterProviderVersion != null)
            {
                await _cacheProvider.RemoveAsync<MasterProviderVersion>(CacheKeys.MasterProviderVersion);
            }

            await _providerVersionMetadataRepositoryPolicy.ExecuteAsync(() => _providerVersionMetadataRepository.UpsertMaster(newMasterProviderVersion));

            return new NoContentResult();
        }

        public async Task<IActionResult> UploadProviderVersion(string actionName,
            string controller,
            string providerVersionId,
            ProviderVersionViewModel providerVersionModel)
        {
            Guard.IsNullOrWhiteSpace(actionName, nameof(actionName));
            Guard.IsNullOrWhiteSpace(controller, nameof(controller));
            Guard.IsNullOrWhiteSpace(providerVersionId, nameof(providerVersionId));
            Guard.ArgumentNotNull(providerVersionModel, nameof(providerVersionModel));

            IActionResult validationResult = await UploadProviderVersionValidate(providerVersionModel, providerVersionId);
            if (validationResult != null) return validationResult;

            ProviderVersion providerVersion = _mapper.Map<ProviderVersion>(providerVersionModel);
            providerVersion.Id = $"providerVersion-{providerVersionId}";

            await UploadProviderVersionBlob(providerVersionId, providerVersion);

            providerVersion.Providers = null;

            ProviderVersionMetadata providerVersionMetadata = providerVersion;

            await _providerVersionMetadataRepositoryPolicy.ExecuteAsync(() => _providerVersionMetadataRepository.CreateProviderVersion(providerVersionMetadata));

            return new CreatedAtActionResult(actionName, controller, new {providerVersionId}, providerVersionId);
        }

        private async Task UploadProviderVersionBlob(string providerVersionId, ProviderVersion providerVersion)
        {
            //HACK this should be in a retry policy too, fix
            ICloudBlob blob = _blobClient.GetBlockBlobReference(providerVersionId.ToLowerInvariant() + ".json");

            // convert string to stream
            byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(providerVersion));

            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                await _blobRepositoryPolicy.ExecuteAsync(() => blob.UploadFromStreamAsync(stream));
            }
        }

        private async Task<IActionResult> UploadProviderVersionValidate(ProviderVersionViewModel providerVersionModel,
            string providerVersionId)
        {
            BadRequestObjectResult validationResult = (await _providerVersionModelValidator.ValidateAsync(providerVersionModel)).PopulateModelState();

            if (validationResult != null) return validationResult;

            if (await Exists(providerVersionId.ToLowerInvariant())) return new ConflictResult();

            if (await Exists(providerVersionModel)) return new ConflictResult();

            return null;
        }

        public async Task<IActionResult> GetProviderVersionMetadata(string providerVersionId)
        {
            Guard.IsNullOrWhiteSpace(providerVersionId, nameof(providerVersionId));

            string cacheKey = $"{CacheKeys.ProviderVersionMetadata}:{providerVersionId}";

            ProviderVersionMetadataDto result = await _cacheProvider.GetAsync<ProviderVersionMetadataDto>(cacheKey);

            if (result == null)
            {
                ProviderVersionMetadata providerVersionMetadata =
                    await _providerVersionMetadataRepositoryPolicy.ExecuteAsync(() => _providerVersionMetadataRepository.GetProviderVersionMetadata(providerVersionId));
                if (providerVersionMetadata != null)
                {
                    result = _mapper.Map<ProviderVersionMetadataDto>(providerVersionMetadata);

                    await _cacheProvider.SetAsync(cacheKey, result);
                }
            }

            if (result == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(result);
        }
    }
}