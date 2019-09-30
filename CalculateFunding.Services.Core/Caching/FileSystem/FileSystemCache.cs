using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using CalculateFunding.Common.Utility;
using Microsoft.Azure.Documents;
using Serilog;
// ReSharper disable InconsistentlySynchronizedField

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public class FileSystemCache : IFileSystemCache
    {
        private readonly ILogger _logger;
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IFileSystemCacheSettings _settings;
        
        private static readonly ConcurrentDictionary<string, object> KeyLocks 
            = new ConcurrentDictionary<string, object>();

        public FileSystemCache(IFileSystemCacheSettings settings,
            IFileSystemAccess fileSystemAccess,
            ILogger logger)
        {
            Guard.ArgumentNotNull(settings, nameof(settings));
            Guard.ArgumentNotNull(fileSystemAccess, nameof(fileSystemAccess));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _settings = settings;
            _fileSystemAccess = fileSystemAccess;
            _logger = logger;
        }

        public bool Exists(FileSystemCacheKey key)
        {
            lock (KeyLockFor(key))
            {
                return _fileSystemAccess.Exists(CachePathForKey(key));
            }
        }
        
        public void Add(FileSystemCacheKey key, Stream contents, CancellationToken cancellationToken = default)
        {
            try
            {
                lock (KeyLockFor(key))
                {
                    _fileSystemAccess.Write(CachePathForKey(key), contents, cancellationToken)
                        .GetAwaiter()
                        .GetResult();
                }
            }
            catch (Exception exception)
            {
                string message = $"Unable to write content for file system cache item with key {key.Key}";
                
                _logger.Error(exception, message);

                throw new Exception(message, exception);
            }
        }

        public Stream Get(FileSystemCacheKey key)
        {
            try
            {
                lock (KeyLockFor(key))
                {
                    return _fileSystemAccess.OpenRead(CachePathForKey(key));
                }
            }
            catch (Exception exception)
            {
                string errorMessage = $"Unable to read content for file system cache item with key {key.Key}";
                
                _logger.Error(errorMessage, exception);
                
                throw new Exception(errorMessage, exception);
            }
        }

        public void EnsureFoldersExist(params string[] folders)
        {
            string settingsPath = _settings.Path;
            
            EnsureFolderExists(settingsPath);

            foreach (string folder in folders)
            {
                EnsureFolderExists(Path.Combine(settingsPath, folder));
            }
        }

        private void EnsureFolderExists(string path)
        {
            if (_fileSystemAccess.FolderExists(path)) return;
            
            _fileSystemAccess.CreateFolder(path);
        }

        private string CachePathForKey(FileSystemCacheKey key)
        {
            return Path.Combine(_settings.Path, key.Path);
        }

        private object KeyLockFor(FileSystemCacheKey key) => KeyLocks.GetOrAdd(key.Key, new object());
    }
}