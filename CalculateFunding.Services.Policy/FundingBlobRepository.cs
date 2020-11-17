using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy
{
    public abstract class FundingBlobRepository : BlobClient
    {
        public FundingBlobRepository(IBlobContainerRepository blobContainerRepository) : base(blobContainerRepository) { }

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

        protected Task<IEnumerable<PublishedFundingTemplate>> Search(string blobNamePrefix)
        {
            List<PublishedFundingTemplate> publishedFundingTemplates = new List<PublishedFundingTemplate>();

            IEnumerable<IListBlobItem> blobItems = ListBlobs(blobNamePrefix, null, true, BlobListingDetails.None);
            foreach (CloudBlockBlob blob in blobItems)
            {
                string templateVersion = Path.GetFileNameWithoutExtension(blob.Name);
                DateTime lastModified = blob.Properties.LastModified.HasValue ?
                    blob.Properties.LastModified.Value.UtcDateTime : blob.Properties.Created.GetValueOrDefault().UtcDateTime;

                publishedFundingTemplates.Add(new PublishedFundingTemplate() { TemplateVersion = templateVersion, PublishDate = lastModified });
            }

            return Task.FromResult<IEnumerable<PublishedFundingTemplate>>(publishedFundingTemplates);
        }
    }
}
