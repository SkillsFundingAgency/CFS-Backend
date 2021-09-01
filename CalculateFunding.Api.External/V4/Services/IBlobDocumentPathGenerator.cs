using CalculateFunding.Services.Core.Caching.FileSystem;

namespace CalculateFunding.Api.External.V4.Services
{
    public interface IBlobDocumentPathGenerator
    {
        string GenerateBlobPathForFundingDocument(string fundingId, int channelId);
        FundingFileSystemCacheKey GenerateFilesystemCacheKeyForFundingDocument(string fundingId, int channelId);
    }
}