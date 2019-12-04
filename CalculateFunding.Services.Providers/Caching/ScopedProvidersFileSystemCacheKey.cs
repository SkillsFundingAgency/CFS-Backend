using CalculateFunding.Services.Core.Caching.FileSystem;

namespace CalculateFunding.Services.Providers.Caching
{
    public class ScopedProvidersFileSystemCacheKey : FileSystemCacheKey

    {
        public ScopedProvidersFileSystemCacheKey(string key)
            : base(key)
        {
        }

        public const string Folder = "scopedProviders";

        public override string Path => $"{Folder}\\{Key}";
    }
}