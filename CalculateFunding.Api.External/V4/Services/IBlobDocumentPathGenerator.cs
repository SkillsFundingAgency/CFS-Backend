using CalculateFunding.Services.Core.Caching.FileSystem;

namespace CalculateFunding.Api.External.V4.Services
{
    public interface IBlobDocumentPathGenerator
    {
        string GenerateBlobPathForFundingDocument(string fundingId, string channelCode);
        FundingFileSystemCacheKey GenerateFilesystemCacheKeyForFundingDocument(string fundingId, string channelCode);
        ProviderFundingFileSystemCacheKey GenerateFilesystemCacheKeyForProviderFundingDocument(string fundingId, string channelCode);
    }
}