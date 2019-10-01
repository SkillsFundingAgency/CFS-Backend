using System;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public class ProviderFundingFileSystemCacheKey : FileSystemCacheKey
    {
        public ProviderFundingFileSystemCacheKey(string key)
            : base(key)
        {
        }

        public const string Folder = "provider";

        public override string Path => $"{Folder}\\{Key}";
    }
}