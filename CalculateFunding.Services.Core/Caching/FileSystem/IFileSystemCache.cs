using System;
using System.IO;
using System.Threading;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public interface IFileSystemCache
    {
        bool Exists(FileSystemCacheKey key);

        void Add(FileSystemCacheKey key, Stream content, CancellationToken cancellationToken = default, bool ensureFolderExists = false);

        void Add(FileSystemCacheKey key, string content, CancellationToken cancellationToken = default, bool ensureFolderExists = false);

        Stream Get(FileSystemCacheKey key);

        void EnsureFoldersExist(params string[] folders);
        
        /// <summary>
        /// Evict all files cached before the supplied date time offset
        /// </summary>
        void Evict(DateTimeOffset before);
    }
}