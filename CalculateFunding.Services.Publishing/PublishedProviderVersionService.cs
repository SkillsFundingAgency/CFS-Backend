using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderVersionService : IPublishedProviderVersionService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly IBlobClient _blobClient;
        private readonly Policy _blobClientPolicy;

        public PublishedProviderVersionService(
            ILogger logger,
            IBlobClient blobClient,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));

            _logger = logger;
            _blobClient = blobClient;
            _blobClientPolicy = resiliencePolicies.BlobClient;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) blobHealth = await _blobClient.IsHealthOk();
         
            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishedProviderVersionService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = blobHealth.Ok, DependencyName = _blobClient.GetType().GetFriendlyName(), Message = blobHealth.Message });
         
            return health;
        }

        public async Task<IActionResult> GetPublishedProviderVersionBody(string publishedProviderVersionId)
        {
            if (string.IsNullOrWhiteSpace(publishedProviderVersionId))
            {
                return new BadRequestObjectResult("Null or empty id provided.");
            }

            string blobName = $"{publishedProviderVersionId}.json";

            bool exists = await _blobClientPolicy.ExecuteAsync(() => _blobClient.BlobExistsAsync($"{publishedProviderVersionId}.json"));

            if (!exists)
            {
                _logger.Error($"Blob '{blobName}' does not exist.");

                return new NotFoundResult();
            }

            string template = string.Empty;

            try
            {
                ICloudBlob blob = await _blobClientPolicy.ExecuteAsync(() => _blobClient.GetBlobReferenceFromServerAsync(blobName));

                using (Stream blobStream = await _blobClientPolicy.ExecuteAsync(() => _blobClient.DownloadToStreamAsync(blob)))
                {
                    using (StreamReader streamReader = new StreamReader(blobStream))
                    {
                        template = await streamReader.ReadToEndAsync();
                    }
                }
            }
            catch(Exception ex)
            {
                string errorMessage = $"Failed to fetch blob '{blobName}' from azure storage";

                _logger.Error(ex, errorMessage);

                return new InternalServerErrorResult(errorMessage);
            }

            return new OkObjectResult(template);
        }

        public async Task SavePublishedProviderVersionBody(string publishedProviderVersionId, string publishedProviderVersionBody)
        {
            Guard.IsNullOrWhiteSpace(publishedProviderVersionId, nameof(publishedProviderVersionId));
            Guard.IsNullOrWhiteSpace(publishedProviderVersionBody, nameof(publishedProviderVersionBody));

            string blobName = $"{publishedProviderVersionId}.json";

            try
            {
                ICloudBlob blob = _blobClient.GetBlockBlobReference(blobName);

                await _blobClientPolicy.ExecuteAsync(() => _blobClient.UploadAsync(blob, publishedProviderVersionBody));
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to save blob '{blobName}' to azure storage";

                _logger.Error(ex, errorMessage);

                throw new Exception(errorMessage, ex);
            }
        }
    }
}
