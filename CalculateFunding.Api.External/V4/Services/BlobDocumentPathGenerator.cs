using CalculateFunding.Services.Core.Caching.FileSystem;

namespace CalculateFunding.Api.External.V4.Services
{
    public class BlobDocumentPathGenerator : IBlobDocumentPathGenerator
    {
        public string GenerateBlobPathForFundingDocument(string fundingId, int channelId)
        {
            // TODO: ensure valid characters are in funding ID.
            // TODO: Change back to channels after writing documents per channel
            //return $"{channelId}/{fundingId}.json";
            return $"{fundingId}.json";
        }

        public FundingFileSystemCacheKey GenerateFilesystemCacheKeyForFundingDocument(string fundingId, int channelId)
        {
            return new FundingFileSystemCacheKey($"{channelId}_{fundingId}");
        }
    }
}
