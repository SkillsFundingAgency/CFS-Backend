using System;
using CalculateFunding.Common.Storage;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Policy.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using Microsoft.Azure.Storage.Blob;

namespace CalculateFunding.Services.Policy
{
    public class FundingTemplateRepository : FundingBlobRepository, IFundingTemplateRepository
    {
        public FundingTemplateRepository(IBlobContainerRepository blobContainerRepository) : base(blobContainerRepository) { }

        public async Task SaveFundingTemplateVersion(string blobName, byte[] templateBytes)
        {
            await SaveVersion(blobName, templateBytes);
        }

        public async Task<bool> TemplateVersionExists(string blobName)
        {
            return await VersionExists(blobName);
        }

        public async Task<string> GetFundingTemplateVersion(string blobName)
        {
            return await GetVersion(blobName);
        }

        public async Task<DateTimeOffset> GetLastModifiedDate(string blobName)
        {
            ICloudBlob blob = await GetBlobReferenceFromServerAsync(blobName);
            
            Guard.Ensure(blob != null,
                $"Didn't locate a blob reference for blob name {blobName}");

            return (blob.Properties?.LastModified ?? blob.Properties?.Created).GetValueOrDefault();
        }

        public async Task<IEnumerable<PublishedFundingTemplate>> SearchTemplates(string blobNamePrefix)
        {
            return await Search(blobNamePrefix);
        }
    }
}
