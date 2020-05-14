using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.Undo.Repositories
{
    public class PublishedFundingUndoBlobStoreRepository : IPublishedFundingUndoBlobStoreRepository
    {
        private readonly AsyncPolicy _resilience;
        private readonly IBlobClient _blobClient;
        private readonly ILogger _logger;

        public PublishedFundingUndoBlobStoreRepository(IBlobClient blobClient, 
            IPublishingResiliencePolicies resiliencePolicies, 
            ILogger logger)
        {
            Guard.ArgumentNotNull(resiliencePolicies?.BlobClient, "resiliencePolicies.BlobClient");
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            
            _resilience = resiliencePolicies.BlobClient;
            _blobClient = blobClient;
            _logger = logger;
        }

        public async Task RemovePublishedProviderVersionBlob(PublishedProviderVersion publishedProviderVersion)
        {
            await RemoveBlob(publishedProviderVersion.FundingId, "publishedproviderversions");
        }

        public async Task RemovePublishedFundingVersionBlob(PublishedFundingVersion publishedFundingVersion)
        {
            await RemoveBlob(publishedFundingVersion.FundingId, "publishedfunding");
        }

        private async Task RemoveBlob(string name, string container)
        {
            try
            {
                ICloudBlob blob = await _resilience.ExecuteAsync(() => 
                    _blobClient.GetBlobReferenceFromServerAsync(name, container));

                await _resilience.ExecuteAsync(() => blob.DeleteIfExistsAsync());
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Unable to delete blob {name} from {container}");   
            }
        }
    }
}