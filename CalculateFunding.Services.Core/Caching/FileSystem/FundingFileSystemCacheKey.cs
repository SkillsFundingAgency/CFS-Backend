namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public class FundingFileSystemCacheKey : FileSystemCacheKey
    {
        public FundingFileSystemCacheKey(string key)
            : base(key)
        {
        }

        public const string Folder = "funding";

        public override string Path => $"{Folder}\\{Key}";
    }
}