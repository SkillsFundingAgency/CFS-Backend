using System;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public class ProviderSourceDatasetFileSystemCacheKey : FileSystemCacheKey
    {
        public ProviderSourceDatasetFileSystemCacheKey(string relationshipId, string providerId, Guid versionKey)
            : base($"{relationshipId}_{providerId}_{versionKey}")
        {
            CachePath = $"providersourcedatasets\\{providerId}\\{relationshipId}_{versionKey}.json";
        }

        public string CachePath;

        public override string Path => CachePath;
    }
}