using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CalculateFunding.Common.Utility;
using Serilog;
using Polly;
// ReSharper disable InconsistentlySynchronizedField

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public class FileSystemCache : IFileSystemCache
    {
        private static readonly object EvictionLock = new object();
        
        private readonly ILogger _logger;
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IFileSystemCacheSettings _settings;
        private volatile bool _evictionInProgress;

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

        public void Evict(DateTimeOffset before)
        {
            lock (EvictionLock)
            {
                if (_evictionInProgress)
                {
                    throw new InvalidOperationException("Eviction already in progress.");
                }

                try
                {
                    _evictionInProgress = true;

                    DeleteAllFilesCachedBefore(before);
                }
                catch (Exception exception)
                {
                    string message = $"Unable to evict all files older than {before} under {_settings.Path}.";

                    _logger.Error(exception, message);   
                }
                finally
                {
                    _evictionInProgress = false;
                }
            }
        }

        private void DeleteAllFilesCachedBefore(DateTimeOffset before)
        {
            IEnumerable<string> filesToEvict = _fileSystemAccess.GetAllFiles(_settings.Path, 
                file => file.CreationTimeUtc < before);

            foreach (string file in filesToEvict)
            {
                try
                {
                    DeleteFile(file);
                }
                catch (Exception exception)
                {
                    string message = $"Unable to evict {file} from file system cache";

                    _logger.Error(exception, message);

                    throw new Exception(message, exception);   
                }
            }
        }

        private void DeleteFile(string filePath)
        {
            Policy.Handle<IOException>()
                .Or<UnauthorizedAccessException>()
                .WaitAndRetry(5, count => TimeSpan.FromMilliseconds(count * 100))
                .Execute(() => _fileSystemAccess.Delete(filePath));
        }

        public bool Exists(FileSystemCacheKey key)
        {
            lock (KeyLockFor(key))
            {
                return _fileSystemAccess.Exists(CachePathForKey(key));
            }
        }

        public void Add(FileSystemCacheKey key, Stream contents, CancellationToken cancellationToken = default, bool ensureFolderExists = false)
        {
            string cachePathForKey = CachePathForKey(key);
            
            try
            {
                lock (KeyLockFor(key))
                {
                    if (ensureFolderExists)
                    {
                        EnsureFolderExists(Path.GetDirectoryName(cachePathForKey));
                    }
                    
                    _fileSystemAccess.Write(cachePathForKey, contents, cancellationToken)
                        .GetAwaiter()
                        .GetResult();
                }
            }
            catch (IOException ioException) when (_fileSystemAccess.Exists(cachePathForKey))
            {
                _logger.Warning("Detected file collision for CachePathForKey(key). Swallowing exception");
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
            var cachePath = key.Path;

            var fileName = Path.GetFileName(cachePath);
            var folder = Path.GetDirectoryName(cachePath);

            return Path.Combine(_settings.Path, folder, $"{_settings.Prefix}_{fileName}");
        }

        private object KeyLockFor(FileSystemCacheKey key)
        {
            if (!_evictionInProgress)
            {
                return KeyLocks.GetOrAdd(key.Key, new object());
            }
            
            lock (EvictionLock)
            {
                return KeyLocks.GetOrAdd(key.Key, new object());
            }
        }
    }
}