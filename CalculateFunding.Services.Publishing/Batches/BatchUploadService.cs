using System;
using System.IO;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.Batches
{
    public class BatchUploadService : IBatchUploadService
    {
        private const string ContainerName = "batchuploads";
        
        private readonly IUniqueIdentifierProvider _batchIdentifiers;
        private readonly IDateTimeProvider _dateTime;
        private readonly IBlobClient _blobClient;
        private readonly ILogger _logger;
        
        private readonly AsyncPolicy _blobResilience;

        public BatchUploadService(IUniqueIdentifierProvider batchIdentifiers,
            IDateTimeProvider dateTime,
            IBlobClient blobClient,
            IPublishingResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(batchIdentifiers, nameof(batchIdentifiers));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(resiliencePolicies?.BlobClient, "resiliencePolicies.BlobClient");
            Guard.ArgumentNotNull(dateTime, nameof(dateTime));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _batchIdentifiers = batchIdentifiers;
            _blobClient = blobClient;
            _dateTime = dateTime;
            _blobResilience = resiliencePolicies.BlobClient;
            _logger = logger;
        }

        public async Task<IActionResult> UploadBatch(BatchUploadRequest uploadRequest)
        {
            Guard.ArgumentNotNull(uploadRequest?.Stream, nameof(uploadRequest.Stream));

            string batchId = _batchIdentifiers.CreateUniqueIdentifier();
            
            await using MemoryStream stream = new MemoryStream(uploadRequest.Stream);

            string blobName = new BatchUploadBlobName(batchId);
            
            ICloudBlob blob = _blobClient.GetBlockBlobReference(blobName, ContainerName);

            await _blobResilience.ExecuteAsync(() => _blobClient.UploadFileAsync(blob, stream));

            _logger.Information($"Uploaded batch ids was allocated batch id {batchId}");

            string saasUrl = _blobClient.GetBlobSasUrl(blobName, 
                _dateTime.UtcNow.AddHours(24), 
                SharedAccessBlobPermissions.Read, 
                ContainerName);
            
            Guard.Ensure(saasUrl.IsNotNullOrWhitespace(), $"Could not create a temporary SAAS Url for batch upload file {blobName}");
            
            return new OkObjectResult(new BatchUploadResponse
            {
                BatchId = batchId,
                Url = saasUrl
            });
        }
    }
}