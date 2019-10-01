using CalculateFunding.Services.Core.Caching.FileSystem;

namespace CalculateFunding.Services.Providers.Caching
{
    public class ProviderVersionFileSystemCacheKey : FileSystemCacheKey

    {
        public ProviderVersionFileSystemCacheKey(string key)
            : base(key)
        {
        }

        public const string Folder = "providerVersion";

        public override string Path => $"{Folder}\\{Key}";
    }
}