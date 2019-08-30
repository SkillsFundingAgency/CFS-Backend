using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    [TestClass]
    public class FundingFileSystemCacheKeyTests
    {
        [TestMethod]
        public void CombinesFundingFolderWithKeyToGivePath()
        {
            string key = new RandomString();

            new FundingFileSystemCacheKey(key)
                .Path
                .Should()
                .Be($"funding\\{key}");
        }
    }
}