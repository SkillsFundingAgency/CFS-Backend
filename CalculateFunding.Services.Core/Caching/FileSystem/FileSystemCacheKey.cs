namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public abstract class FileSystemCacheKey
    {
        protected FileSystemCacheKey(string key,
            string folder)
        {
            Key = key;
            Path = $"{folder}\\{key}";
        }

        public string Key { get; }

        public virtual string Path { get; }
    }
}