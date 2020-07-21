using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using CalculateFunding.Tests.Common.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Dapper;

namespace CalculateFunding.Services.FundingDataZone.UnitTests
{
    [TestClass]
    public class PublishingAreaRepositoryTests
    {
        private Mock<ISqlConnectionFactory> _connectionFactory;
        private Mock<IDbConnection> _connection;

        private PublishingAreaRepository _repository;

        [TestInitialize]
        public void SetUp()
        {
            _connectionFactory = new Mock<ISqlConnectionFactory>();
            _connection = new Mock<IDbConnection>();

            _connectionFactory.Setup(_ => _.CreateConnection())
                .Returns(_connection.Object);

            _repository = new PublishingAreaRepository(_connectionFactory.Object);
        }

        [TestMethod]
        public async Task GetProvidersInSnapshot()
        {
            PublishingAreaProvider[] expectedPublishAreaProviders =
            {
                NewPublishingAreaProvider(), NewPublishingAreaProvider(), NewPublishingAreaProvider()
            };

            int snapShotId = NewRandomNumber();
            
            GivenTheDapperReturnFor("sp_getProviderDetailsBySnapshotId", 
                _ => _.ProviderSnapshotId == snapShotId, 
                expectedPublishAreaProviders);

            IEnumerable<PublishingAreaProvider> actualPublishAreaProviders = await WhenTheProvidersInSnapshotAreQueried(snapShotId);

            actualPublishAreaProviders
                .Should()
                .BeEquivalentTo<PublishingAreaProvider>(expectedPublishAreaProviders);
        }

        private async Task<IEnumerable<PublishingAreaProvider>> WhenTheProvidersInSnapshotAreQueried(int snapshotId)
            => await _repository.GetProvidersInSnapshot(snapshotId);

        private void GivenTheDapperReturnFor<TReturn>(string sql,
            Func<dynamic, bool> parameterConstraint,
            IEnumerable<TReturn> items)
        {
            _connection.SetupDapperAsync(_ => _.QueryAsync<TReturn>(sql,
                    It.Is<object>(prm => parameterConstraint(prm)),
                    null,
                    null,
                    CommandType.StoredProcedure))
                .ReturnsAsync(items);
        }
        
        private int NewRandomNumber() => new RandomNumberBetween(1, int.MaxValue);

        private PublishingAreaProvider NewPublishingAreaProvider(Action<PublishingAreaProviderBuilder> setUp = null)
        {
            PublishingAreaProviderBuilder publishingAreaProviderBuilder = new PublishingAreaProviderBuilder();

            setUp?.Invoke(publishingAreaProviderBuilder);

            return publishingAreaProviderBuilder.Build();
        }
    }
}