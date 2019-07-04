using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using Microsoft.Azure.Storage.Blob;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy
{
    public abstract class FundingBlobRepository : BlobClient
    {
        public FundingBlobRepository(BlobStorageOptions blobStorageOptions) : base(blobStorageOptions) { }

        protected async Task SaveVersion(string blobName, byte[] fileBytes)
        {
            Guard.IsNullOrWhiteSpace(blobName, nameof(blobName));
            Guard.ArgumentNotNull(fileBytes, nameof(fileBytes));

            if (fileBytes.Length == 0)
            {
                throw new ArgumentException("Empty schema bytes provided");
            }

            ICloudBlob blockBlob = GetBlockBlobReference(blobName);

            using (MemoryStream memoryStream = new MemoryStream(fileBytes))
            {
                await blockBlob.UploadFromStreamAsync(memoryStream);
            }
        }

        protected async Task<bool> VersionExists(string blobName)
        {
            ICloudBlob blockBlob = GetBlockBlobReference(blobName);

            return await blockBlob.ExistsAsync();
        }

        protected async Task<string> GetVersion(string blobName)
        {
            Guard.IsNullOrWhiteSpace(blobName, nameof(blobName));

            ICloudBlob blob = GetBlockBlobReference(blobName);

            if (blob == null)
            {
                throw new Exception($"Failed to refrence blob: '{blobName}'");
            }

            string schema = string.Empty;

            using (MemoryStream schemaStream = (MemoryStream)await DownloadToStreamAsync(blob))
            {
                if (schemaStream == null || schemaStream.Length == 0)
                {
                    throw new Exception($"Invalid blob returned: {blobName}");
                }

                schema = Encoding.UTF8.GetString(schemaStream.ToArray());
            }

            return schema;
        }
    }
}
