namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public interface IFileSystemCacheSettings
    {
        string Path { get; }
        
        string Prefix { get; }
    }
}