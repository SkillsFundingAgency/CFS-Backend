using System;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public class FileSystemCacheSettings : IFileSystemCacheSettings
    {
        public string Path { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }
}