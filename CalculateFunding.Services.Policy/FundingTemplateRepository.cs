using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Policy.Interfaces;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy
{
    public class FundingTemplateRepository : FundingBlobRepository, IFundingTemplateRepository
    {
        public FundingTemplateRepository(BlobStorageOptions blobStorageOptions, IBlobContainerRepository blobContainerRepository) : base(blobStorageOptions, blobContainerRepository) { }

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
    }
}
