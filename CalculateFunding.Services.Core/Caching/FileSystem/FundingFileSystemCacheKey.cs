namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public class FundingFileSystemCacheKey : FileSystemCacheKey
    {
        public const string Folder = "funding";

        public FundingFileSystemCacheKey(string key)
            : base(key, Folder)
        {
        }
    }
}