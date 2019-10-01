using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    [TestClass]
    public class ProviderFundingFileSystemCacheKeyTests
    {
        [TestMethod]
        public void CombinesFundingFolderWithKeyToGivePath()
        {
            string key = new RandomString();

            new ProviderFundingFileSystemCacheKey(key)
                .Path
                .Should()
                .Be($"provider\\{key}");
        }
    }
}