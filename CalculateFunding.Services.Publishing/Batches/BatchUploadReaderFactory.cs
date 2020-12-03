using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Batches
{
    public class BatchUploadReaderFactory : IBatchUploadReaderFactory
    {
        private readonly IBlobClient _blobClient;
        private readonly IPublishingResiliencePolicies _resiliencePolicies;

        public BatchUploadReaderFactory(IBlobClient blobClient,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            
            _blobClient = blobClient;    
            _resiliencePolicies = resiliencePolicies;
        }

        public IBatchUploadReader CreateBatchUploadReader()
            => new BatchUploadReader(_blobClient, _resiliencePolicies);
    }
}