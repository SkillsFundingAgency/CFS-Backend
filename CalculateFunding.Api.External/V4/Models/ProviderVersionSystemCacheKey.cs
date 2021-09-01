using CalculateFunding.Services.Core.Caching.FileSystem;

namespace CalculateFunding.Api.External.V4.Models
{
    public class ProviderVersionSystemCacheKey : FileSystemCacheKey
    {
        public ProviderVersionSystemCacheKey(string key)
            : base(key, "providerVersion")
        {
        }
    }
}
