using CalculateFunding.Services.Core.Caching.FileSystem;

namespace CalculateFunding.Api.External.V4.Services
{
    public class BlobDocumentPathGenerator : IBlobDocumentPathGenerator
    {
        public string GenerateBlobPathForFundingDocument(string fundingId, string channelCode)
        {
            return $"{channelCode}/{fundingId}.json";
        }

        public FundingFileSystemCacheKey GenerateFilesystemCacheKeyForFundingDocument(string fundingId, string channelCode)
        {
            return new FundingFileSystemCacheKey($"{channelCode}_{fundingId}");
        }

        public ProviderFundingFileSystemCacheKey GenerateFilesystemCacheKeyForProviderFundingDocument(string fundingId, string channelCode)
        {
            return new ProviderFundingFileSystemCacheKey($"{channelCode}_{fundingId}");
        }
    }
}
