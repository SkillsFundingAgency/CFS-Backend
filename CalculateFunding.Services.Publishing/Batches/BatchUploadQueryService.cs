using System.IO;
using System.Threading.Tasks;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.Batches
{
    public class BatchUploadQueryService : IBatchUploadQueryService
    {
        private const string ContainerName = "batchuploads";
        
        private readonly IBlobClient _blobClient;
        private readonly AsyncPolicy _blobResilience;
        private readonly ILogger _logger;

        public BatchUploadQueryService(IBlobClient blobClient,
            IPublishingResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(resiliencePolicies?.BlobClient, "resiliencePolicies.BlobClient");
            Guard.ArgumentNotNull(logger, nameof(logger));
            
            _blobClient = blobClient;
            _blobResilience = resiliencePolicies.BlobClient;
            _logger = logger;
        }

        public async Task<IActionResult> GetBatchProviderIds(string batchId)
        {
            Guard.IsNullOrWhiteSpace(batchId, nameof(batchId));
            
            BatchUploadProviderIdsBlobName providerIdsBlobName = new BatchUploadProviderIdsBlobName(batchId);

            await using Stream stream = await BatchStream(providerIdsBlobName);

            if (stream == null)
            {
                _logger.Warning($"Didn't locate the batch provider ids for {batchId}");
                
                return new NotFoundResult();
            }

            string[] publishedProviderIds = stream.AsPoco<string[]>();

            return new OkObjectResult(publishedProviderIds);
        }
        
        private async Task<Stream> BatchStream(string blobName)
        {
            ICloudBlob blob = await _blobClient.GetBlobReferenceFromServerAsync(blobName, ContainerName);
            
            return await _blobResilience.ExecuteAsync(() => _blobClient.DownloadToStreamAsync(blob));
        }
    }
}