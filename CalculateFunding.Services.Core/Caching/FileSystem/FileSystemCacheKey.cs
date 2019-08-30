namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public abstract class FileSystemCacheKey
    {
        protected FileSystemCacheKey(string key)
        {
            Key = key;
        }

        public string Key { get; }

        public abstract string Path { get; }
    }
}