using System;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public class ProviderFileSystemCacheKey : FileSystemCacheKey
    {
        public ProviderFileSystemCacheKey(string key)
            : base(key)
        {
        }

        public const string Folder = "provider";

        public override string Path => $"{Folder}\\{Key}";
    }
}