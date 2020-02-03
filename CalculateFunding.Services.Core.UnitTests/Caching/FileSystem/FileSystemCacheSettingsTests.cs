using System;
using CalculateFunding.Common.Caching;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    [TestClass]
    public class FileSystemCacheSettingsTests
    {
        private Mock<IConfiguration> _configuration;
        private Mock<ICacheProvider> _cacheProvider;
        
        private FileSystemCacheSettings _fileSystemCacheSettings;

        [TestInitialize]
        public void SetUp()
        {
            _configuration = new Mock<IConfiguration>();
            _cacheProvider = new Mock<ICacheProvider>();

            _fileSystemCacheSettings = new FileSystemCacheSettings(_configuration.Object,
                _cacheProvider.Object);
        }
        
        [TestMethod]
        public void DefaultPathToAppData()
        {
            _fileSystemCacheSettings
                .Path
                .Should()
                .Be(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        }
        
        [TestMethod]
        public void ReadsConfiguredPathIfPresent()
        {
            string configuredPath = NewRandomString();
            
            GivenTheConfigurationValue(nameof(IFileSystemCacheSettings.Path), configuredPath);
            
            _fileSystemCacheSettings
                .Path
                .Should()
                .Be(configuredPath);
        }
        
        [TestMethod]
        public void ReadsConfiguredPrefixKeyIfPresent()
        {
            string configuredPrefix = NewRandomString();
            
            GivenTheConfigurationValue(nameof(IFileSystemCacheSettings.Prefix), configuredPrefix);
            
            _fileSystemCacheSettings
                .Prefix
                .Should()
                .Be(configuredPrefix);
        }
        
        [TestMethod]
        public void ReadsCachedPrefixKeyIfNotPresentInAppConfiguration()
        {
            string cachedPrefix = NewRandomString();
            
            GivenTheCachedValue(nameof(IFileSystemCacheSettings.Prefix), cachedPrefix);
            
            _fileSystemCacheSettings
                .Prefix
                .Should()
                .Be(cachedPrefix);
        }
        
        [TestMethod]
        public void CreatesNewCachedPrefixKeyIfNotPresentInAppConfigurationOrRedidCache()
        {
            string generatedPrefix = _fileSystemCacheSettings
                .Prefix;
            
            _cacheProvider
                .Verify(_ => _.SetAsync($"{FileSystemCacheSettings.SectionName}:{nameof(IFileSystemCacheSettings.Prefix)}", 
                        generatedPrefix, 
                        null),
                    Times.Once);
        }

        private void GivenTheConfigurationValue(string key, string value)
        {
            _configuration
                .Setup(_ => _[$"{FileSystemCacheSettings.SectionName}:{key}"])
                .Returns(value);
        }

        private void GivenTheCachedValue(string key, string value)
        {
            _cacheProvider
                .Setup(_ => _.GetAsync<string>($"{FileSystemCacheSettings.SectionName}:{key}", 
                    null))
                .ReturnsAsync(value);
        }
        
        private string NewRandomString() => new RandomString();
    }
}