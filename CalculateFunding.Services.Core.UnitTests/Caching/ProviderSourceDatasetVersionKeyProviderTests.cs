using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Core.Caching
{
    [TestClass]
    public class ProviderSourceDatasetVersionKeyProviderTests
    {
        private ICacheProvider _cacheProvider;
        private ProviderSourceDatasetVersionKeyProvider _provider;

        [TestInitialize]
        public void SetUp()
        {
            _cacheProvider = Substitute.For<ICacheProvider>();
            _provider = new ProviderSourceDatasetVersionKeyProvider(_cacheProvider);
        }
        
        [TestMethod]
        public async Task GetsVersionKeyFromRedisForRelationshipId()
        {
            string relationshipId = new RandomString();
            Guid expectedKey = Guid.NewGuid();
            
            GivenTheVersionKeyForRelationship(relationshipId, expectedKey);

            (await _provider.GetProviderSourceDatasetVersionKey(relationshipId))
                .Should()
                .Be(expectedKey);
        }
        
        [TestMethod]
        public async Task CachesVersionKeyToRedisForRelationshipId()
        {
            string relationshipId = new RandomString();
            Guid expectedKey = Guid.NewGuid();

            await _provider.AddOrUpdateProviderSourceDatasetVersionKey(relationshipId, expectedKey);

            await _cacheProvider
                .Received(1)
                .SetAsync(KeyFor(relationshipId), expectedKey);
        }

        private void GivenTheVersionKeyForRelationship(string relationshipId, Guid key)
        {
            _cacheProvider.GetAsync<Guid>(KeyFor(relationshipId))
                .Returns(key);
        }

        private static string KeyFor(string relationshipId)
        {
            return $"ProviderDatasetVersion:{relationshipId}";
        }
    }
}