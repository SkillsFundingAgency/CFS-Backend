using CalculateFunding.Services.Core.Caching.FileSystem;

namespace CalculateFunding.Services.Providers.Caching
{
    public class ProviderVersionFileSystemCacheKey : FileSystemCacheKey
    {
        public const string Folder = "providerVersion";

        public ProviderVersionFileSystemCacheKey(string key)
            : base(key, Folder)
        {
        }
    }
}