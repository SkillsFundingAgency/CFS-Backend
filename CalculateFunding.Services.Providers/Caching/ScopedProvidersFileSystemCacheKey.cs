using CalculateFunding.Services.Core.Caching.FileSystem;

namespace CalculateFunding.Services.Providers.Caching
{
    public class ScopedProvidersFileSystemCacheKey : FileSystemCacheKey

    {
        public ScopedProvidersFileSystemCacheKey(string specificationId,string cacheGuid)
            : base($"scopedproviders_{specificationId}_{cacheGuid}")
        {
        }

        public const string Folder = "scopedProviders";

        public override string Path => $"{Folder}\\{Key}";
    }
}