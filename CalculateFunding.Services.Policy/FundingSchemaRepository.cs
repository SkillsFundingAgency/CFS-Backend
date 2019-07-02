using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.Azure.Storage.Blob;

namespace CalculateFunding.Services.Policy
{
    public class FundingSchemaRepository : BlobClient, IFundingSchemaRepository
    {
        public FundingSchemaRepository(BlobStorageOptions blobStorageOptions) : base(blobStorageOptions) { }

        public async Task SaveFundingSchemaVersion(string blobName, byte[] schemaBytes)
        {
            Guard.IsNullOrWhiteSpace(blobName, nameof(blobName));
            Guard.ArgumentNotNull(schemaBytes, nameof(schemaBytes));

            if(schemaBytes.Length == 0)
            {
                throw new ArgumentException("Empty schema bytes provided");
            }

            ICloudBlob blockBlob = GetBlockBlobReference(blobName);

            using (MemoryStream memoryStream = new MemoryStream(schemaBytes))
            {
                await blockBlob.UploadFromStreamAsync(memoryStream);
            }
        }

        public async Task<bool> Exists(string blobName)
        {
            ICloudBlob blockBlob = GetBlockBlobReference(blobName);

            return await blockBlob.ExistsAsync();
        }

        public async Task<string> GetFundingSchemaVersion(string blobName)
        {
            Guard.IsNullOrWhiteSpace(blobName, nameof(blobName));

            ICloudBlob blob = GetBlockBlobReference(blobName);

            if(blob == null)
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
