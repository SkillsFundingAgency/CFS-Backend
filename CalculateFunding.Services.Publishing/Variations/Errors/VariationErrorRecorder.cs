using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing.Variations.Errors
{
    public class VariationErrorRecorder : IRecordVariationErrors
    {
        private readonly Policy _resiliencePolicy;
        private readonly IBlobClient _blobClient;

        private const string ContainerName = "publishedproviderversions";
        private const string BlobPrefix = "variationerrors";

        public VariationErrorRecorder(IPublishingResiliencePolicies resiliencePolicies, IBlobClient blobClient)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(resiliencePolicies?.BlobClient, nameof(resiliencePolicies.BlobClient));
            
            _resiliencePolicy = resiliencePolicies.BlobClient;
            _blobClient = blobClient;
        }

        public async Task RecordVariationErrors(IEnumerable<string> variationErrors, string specificationId)
        {
            Guard.ArgumentNotNull(variationErrors, nameof(variationErrors));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            string blobName = $"{BlobPrefix}_{specificationId}.csv";
            string errorsCsv = variationErrors.Join("\n");

            await _resiliencePolicy.ExecuteAsync(() => _blobClient.UploadFileAsync(blobName, errorsCsv, ContainerName));
        }
    }
}