namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public class ProviderFundingFileSystemCacheKey : FileSystemCacheKey
    {
        public const string Folder = "provider";

        public ProviderFundingFileSystemCacheKey(string key)
            : base(key, Folder)
        {
        }
    } 
}