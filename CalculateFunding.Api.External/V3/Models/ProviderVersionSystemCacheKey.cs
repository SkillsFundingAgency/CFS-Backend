using CalculateFunding.Services.Core.Caching.FileSystem;

namespace CalculateFunding.Api.External.V3.Models
{
    public class ProviderVersionSystemCacheKey : FileSystemCacheKey
    {
        public ProviderVersionSystemCacheKey(string key)
            : base(key, "providerVersion")
        {
        }
    }
}
