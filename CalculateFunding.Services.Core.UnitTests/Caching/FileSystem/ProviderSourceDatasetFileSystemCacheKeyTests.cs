using System;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    [TestClass]
    public class ProviderSourceDatasetFileSystemCacheKeyTests
    {
        [TestMethod]
        public void CombinesFundingFolderWithProviderRelationshipAndVersionIdsToGivePath()
        {
            string providerId = new RandomString();
            string relationshipId = new RandomString();
            Guid versionKey = Guid.NewGuid();

            new ProviderSourceDatasetFileSystemCacheKey(relationshipId, providerId, versionKey)
                .Path
                .Should()
                .Be($"providersourcedatasets\\{providerId}\\{relationshipId}_{versionKey}.json");
        }
    }
}