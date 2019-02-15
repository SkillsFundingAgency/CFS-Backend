using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.Results.Repositories
{
    public class ProviderVariationsStorageRepository : BlobClient, IProviderVariationsStorageRepository
    {
        public ProviderVariationsStorageRepository(BlobStorageOptions blobStorageOptions) : base(blobStorageOptions) { }

        public async Task<string> SaveErrors(string specificationId, string jobId, IEnumerable<ProviderVariationError> errors)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            string blobName = $"{specificationId}/{jobId}/{DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-fff")}.json".ToLowerInvariant();

            return await UploadFileAsync(blobName, errors);
        }
    }
}
