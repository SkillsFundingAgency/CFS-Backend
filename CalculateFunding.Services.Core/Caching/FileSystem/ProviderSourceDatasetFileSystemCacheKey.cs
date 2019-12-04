using System;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public class ProviderSourceDatasetFileSystemCacheKey : FileSystemCacheKey
    {
        public ProviderSourceDatasetFileSystemCacheKey(string relationshipId, string providerId, Guid versionKey) 
            : base($"{relationshipId}_{providerId}_{versionKey}")
        {
        }
        
        public const string Folder = "providersourcedatasets";
        
        public override string Path => $"{Folder}\\{Key}.json";
    }
}