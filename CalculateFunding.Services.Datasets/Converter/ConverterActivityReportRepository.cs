using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Datasets.Interfaces;
using Polly;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;

namespace CalculateFunding.Services.Datasets.Converter
{
    public class ConverterActivityReportRepository : IConverterActivityReportRepository
    {
        private readonly IBlobClient _blobClient;
        private readonly AsyncPolicy _blobClientPolicy;

        public ConverterActivityReportRepository(IBlobClient blobClient,
            IDatasetsResiliencePolicies datasetsResiliencePolicies)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(datasetsResiliencePolicies.BlobClient, nameof(datasetsResiliencePolicies.BlobClient));

            _blobClient = blobClient;
            _blobClientPolicy = datasetsResiliencePolicies.BlobClient;
        }

        public async Task UploadReport(string filename, string prettyFilename, Stream csvFileStream, IDictionary<string, string> metadata)
        {
            ICloudBlob blob = _blobClient.GetBlockBlobReference(filename);
            blob.Properties.ContentDisposition = $"attachment; filename={prettyFilename}";

            await _blobClientPolicy.ExecuteAsync(() => UploadBlob(blob, csvFileStream, metadata));
        }

        private async Task UploadBlob(ICloudBlob blob, Stream csvFileStream, IDictionary<string, string> metadata)
        {
            await _blobClient.UploadAsync(blob, csvFileStream);
            await _blobClient.AddMetadataAsync(blob, metadata);
        }
    }
}
