using System;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public class ProviderSourceDatasetFileSystemCacheKey : FileSystemCacheKey
    {
        public ProviderSourceDatasetFileSystemCacheKey(string relationshipId, string providerId, Guid versionKey)
            : base($"{relationshipId}_{versionKey}.json", $"providersourcedatasets\\{providerId}")
        {
        }
    }
}