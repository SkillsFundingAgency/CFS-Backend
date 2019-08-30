using System.IO;
using System.Threading;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public interface IFileSystemCache
    {
        bool Exists(FileSystemCacheKey key);

        void Add(FileSystemCacheKey key, Stream content, CancellationToken cancellationToken = default);

        Stream Get(FileSystemCacheKey key);

        void EnsureFoldersExist();
    }
}