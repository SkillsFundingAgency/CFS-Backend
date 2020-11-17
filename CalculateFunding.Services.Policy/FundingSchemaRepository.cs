using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Policy.Interfaces;

namespace CalculateFunding.Services.Policy
{
    public class FundingSchemaRepository : FundingBlobRepository, IFundingSchemaRepository
    {
        public FundingSchemaRepository(IBlobContainerRepository blobContainerRepository) : base(blobContainerRepository) { }

        public async Task SaveFundingSchemaVersion(string blobName, byte[] schemaBytes)
        {
            await SaveVersion(blobName, schemaBytes);
        }

        public async Task<bool> SchemaVersionExists(string blobName)
        {
            return await VersionExists(blobName);
        }

        public async Task<string> GetFundingSchemaVersion(string blobName)
        {
            return await GetVersion(blobName);
        }
    }
}
