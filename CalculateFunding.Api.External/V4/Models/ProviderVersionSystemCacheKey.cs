using CalculateFunding.Services.Core.Caching.FileSystem;

namespace CalculateFunding.Api.External.V4.Models
{
    public class ProviderVersionSystemCacheKey : FileSystemCacheKey
    {
        public const string Folder = "providerVersion";

        public ProviderVersionSystemCacheKey(string key)
            : base(key, Folder)
        {
        }
    }
}
