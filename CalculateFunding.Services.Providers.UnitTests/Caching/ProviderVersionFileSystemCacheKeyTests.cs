using CalculateFunding.Services.Providers.Caching;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Providers.UnitTests.Caching
{
    [TestClass]
    public class ProviderVersionFileSystemCacheKeyTests
    {
        [TestMethod]
        public void CombinesFundingFolderWithKeyToGivePath()
        {
            string key = new RandomString();

            new ProviderVersionFileSystemCacheKey(key)
                .Path
                .Should()
                .Be($"providerVersion\\{key}");
        }
    }
}