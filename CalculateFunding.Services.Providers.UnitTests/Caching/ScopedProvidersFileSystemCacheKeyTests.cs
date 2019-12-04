using CalculateFunding.Services.Providers.Caching;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CalculateFunding.Services.Providers.UnitTests.Caching
{
    [TestClass]
    public class ScopedProvidersFileSystemCacheKeyTests
    {
        [TestMethod]
        public void CombinesScopedProvidersFolderWithSpecificationIdAndCacheGuidToGivePath()
        {            
            string specificationId = new RandomString();
            string cacheGuid = Guid.NewGuid().ToString();

            new ScopedProvidersFileSystemCacheKey(specificationId, cacheGuid)
                .Path
                .Should()
                .Be($"{ScopedProvidersFileSystemCacheKey.Folder}\\scopedproviders_{specificationId}_{cacheGuid}");
        }
    }
}