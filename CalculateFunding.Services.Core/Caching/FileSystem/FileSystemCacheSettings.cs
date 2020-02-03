using System;
using System.Runtime.CompilerServices;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Utility;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    public class FileSystemCacheSettings : IFileSystemCacheSettings
    {
        public static readonly string SectionName = nameof(FileSystemCacheSettings).ToLower();
        
        private static readonly string _defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        private readonly ICacheProvider _cacheProvider;
        private readonly IConfiguration _configuration;

        public FileSystemCacheSettings(IConfiguration configuration, 
            ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(configuration, nameof(configuration));
            
            _configuration = configuration;
            _cacheProvider = cacheProvider;
        }

        public string Path => GetConfigurationValue() ?? _defaultPath;
        
        public string Prefix => GetConfigurationValue() ?? GetOrCreateCachedPrefix();

        private string GetConfigurationValue([CallerMemberName] string key = null) => _configuration[$"{SectionName}:{key}"];

        private string GetOrCreateCachedPrefix()
        {
            string key = $"{SectionName}:{nameof(Prefix)}";
            
            string cachedPrefix = _cacheProvider.GetAsync<string>(key)
                .GetAwaiter()
                .GetResult();

            if (cachedPrefix.IsNullOrWhitespace())
            {
                cachedPrefix = Guid.NewGuid().ToString();
                
                _cacheProvider.SetAsync(key, cachedPrefix)
                    .GetAwaiter()
                    .GetResult();
            }

            return cachedPrefix;
        }
    }
}