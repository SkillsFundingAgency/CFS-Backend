using System;
using System.IO;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Services.CalcEngine.Caching
{
    public class SpecificationAssemblyProvider : ISpecificationAssemblyProvider
    {
        private readonly IFileSystemCache _fileSystemCache;
        private readonly IBlobClient _blobs;
        private readonly AsyncPolicy _blobResilience;
        private readonly ILogger _logger;

        public SpecificationAssemblyProvider(IFileSystemCache fileSystemCache,
            IBlobClient blobs,
            ICalculatorResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(fileSystemCache, nameof(fileSystemCache));
            Guard.ArgumentNotNull(blobs, nameof(blobs));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies?.BlobClient, nameof(resiliencePolicies.BlobClient));
            
            _fileSystemCache = fileSystemCache;
            _blobs = blobs;
            _logger = logger;
            _blobResilience = resiliencePolicies.BlobClient;
        }

        public async Task<Stream> GetAssembly(string specificationId,
            string etag)
        {
            try
            {
                Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
                Guard.IsNullOrWhiteSpace(etag, nameof(etag));

                SpecificationAssemblyFileSystemCacheKey fileSystemCacheKey = GetFileSystemCacheKey(specificationId, etag);

                if (_fileSystemCache.Exists(fileSystemCacheKey))
                {
                    return _fileSystemCache.Get(fileSystemCacheKey);
                }

                (Stream assembly, string etag) specificationAssemblyDetails = await GetAssemblyFromBlobStorage(specificationId);

                if (specificationAssemblyDetails.etag.IsNotNullOrWhitespace())
                {
                    if (!etag.Equals(specificationAssemblyDetails.etag, StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new NonRetriableException("Invalid specification assembly etag.");
                    }
                }

                await SetAssembly(specificationId, specificationAssemblyDetails.assembly);

                return specificationAssemblyDetails.assembly;
            }
            catch (Exception exception)
            {
                LogError(exception, $"Unable to fetch specification assembly for specification {specificationId} with etag {etag}");
                
                throw;
            }
        }

        public async Task SetAssembly(string specificationId, Stream assembly)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(assembly, nameof(assembly));

            ICloudBlob assemblyBlob = await GetAssemblyCloudBlob(specificationId);
            
            Guard.ArgumentNotNull(assemblyBlob, nameof(assemblyBlob));

            await assemblyBlob.FetchAttributesAsync();
            
            _fileSystemCache.Add(GetFileSystemCacheKey(specificationId, assemblyBlob.Properties.ETag), assembly, ensureFolderExists: true);
        }

        private async Task<(Stream assembly, string etag)> GetAssemblyFromBlobStorage(string specificationId)
        {
            ICloudBlob assemblyBlob = await GetAssemblyCloudBlob(specificationId);

            if (assemblyBlob == null)
            {
                return (null, null);
            }

            Stream assembly = await _blobResilience.ExecuteAsync(() => _blobs.DownloadToStreamAsync(assemblyBlob));

            return (assembly, assemblyBlob.Properties.ETag);
        }

        private Task<ICloudBlob> GetAssemblyCloudBlob(string specificationId)
        {
            return _blobResilience.ExecuteAsync(() => _blobs.GetBlobReferenceFromServerAsync(GetBlobNameForSpecificationAssembly(specificationId)));
        }

        private static string GetBlobNameForSpecificationAssembly(string specificationId) 
            => $"{specificationId}/implementation.dll";

        private SpecificationAssemblyFileSystemCacheKey GetFileSystemCacheKey(string specificationId,
            string etag)
            => new SpecificationAssemblyFileSystemCacheKey(specificationId, etag);
        
        private void LogError(Exception exception,
            string message) => _logger.Error(exception, $"{nameof(SpecificationAssemblyProvider)} {message}");
    }
}