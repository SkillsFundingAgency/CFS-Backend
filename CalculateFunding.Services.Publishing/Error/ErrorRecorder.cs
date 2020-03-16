using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Error
{
    public abstract class ErrorRecorder
    {
        private readonly Policy _resiliencePolicy;
        private readonly IBlobClient _blobClient;

        public ErrorRecorder(IPublishingResiliencePolicies resiliencePolicies, IBlobClient blobClient)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(resiliencePolicies?.BlobClient, nameof(resiliencePolicies.BlobClient));

            _resiliencePolicy = resiliencePolicies.BlobClient;
            _blobClient = blobClient;
        }

        protected async Task RecordErrors(string blobName, IEnumerable<string> errors, string containerName)
        {
            await _resiliencePolicy.ExecuteAsync(() => _blobClient.UploadFileAsync($"{blobName}.csv", errors.Join("/n"), containerName));
        }
    }
}
