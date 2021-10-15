using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class PublishedProviderChannelVersionService : IPublishedProviderChannelVersionService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly IBlobClient _blobClient;

        private readonly AsyncPolicy _blobClientPolicy;

        private const string ContainerName = "releasedproviders";

        public PublishedProviderChannelVersionService(
            ILogger logger,
            IBlobClient blobClient,
            IPublishingResiliencePolicies publishingResiliencePolicies)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(logger, nameof(logger));

            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(publishingResiliencePolicies.BlobClient, nameof(publishingResiliencePolicies.BlobClient));

            _blobClient = blobClient;
            _blobClientPolicy = publishingResiliencePolicies.BlobClient;
            _logger = logger;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = await _blobClient.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishedProviderVersionService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = _blobClient.GetType().GetFriendlyName(), Message = Message });

            return health;
        }

        public async Task SavePublishedProviderVersionBody(
            string publishedProviderVersionId,
            string publishedProviderVersionBody,
            string specificationId,
            string channelCode)
        {
            Guard.IsNullOrWhiteSpace(publishedProviderVersionId, nameof(publishedProviderVersionId));
            Guard.IsNullOrWhiteSpace(publishedProviderVersionBody, nameof(publishedProviderVersionBody));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            string blobName = $"{channelCode}/{publishedProviderVersionId}.json";

            try
            {
                ICloudBlob blob = _blobClient.GetBlockBlobReference(blobName);

                await _blobClientPolicy.ExecuteAsync(() => UploadBlob(blob, publishedProviderVersionBody, GetMetadata(specificationId)));
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to save blob '{blobName}' to azure storage";

                _logger.Error(ex, errorMessage);

                throw new Exception(errorMessage, ex);
            }
        }

        private async Task UploadBlob(ICloudBlob blob, string contents, IDictionary<string, string> metadata)
        {
            await _blobClient.UploadFileAsync(blob.Name, contents, ContainerName);
            await _blobClient.AddMetadataAsync(blob, metadata);
        }

        private IDictionary<string, string> GetMetadata(string specificationId)
        {
            return new Dictionary<string, string>
            {
                { "specification-id", specificationId }
            };
        }
    }
}
