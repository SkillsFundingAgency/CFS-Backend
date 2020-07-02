using CalculateFunding.Services.Core.Caching.FileSystem;

namespace CalculateFunding.Api.External.V3.Models
{
    public class ProviderVersionSystemCacheKey : FileSystemCacheKey
    {
        public ProviderVersionSystemCacheKey(string key)
            : base(key)
        {
        }

        public const string Folder = "providerVersion";

        public override string Path => $"{Folder}\\{Key}";
    }
}
