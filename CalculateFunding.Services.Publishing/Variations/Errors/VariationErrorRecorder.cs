using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Error;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing.Variations.Errors
{
    public class VariationErrorRecorder : ErrorRecorder, IRecordVariationErrors
    {
        private const string ContainerName = "publishedproviderversions";
        private const string BlobPrefix = "variationerrors";

        public VariationErrorRecorder(IPublishingResiliencePolicies resiliencePolicies, IBlobClient blobClient) : base(resiliencePolicies, blobClient)
        {
        }

        public async Task RecordVariationErrors(IEnumerable<string> variationErrors, string specificationId)
        {
            Guard.ArgumentNotNull(variationErrors, nameof(variationErrors));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            string blobName = $"{BlobPrefix}_{specificationId}";

            await RecordErrors(blobName, variationErrors, ContainerName);
        }
    }
}