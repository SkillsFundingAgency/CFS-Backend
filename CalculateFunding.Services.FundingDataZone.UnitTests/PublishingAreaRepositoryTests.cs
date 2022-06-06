using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using CalculateFunding.Tests.Common.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Dapper;
using Polly;

namespace CalculateFunding.Services.FundingDataZone.UnitTests
{
    [TestClass]
    public class PublishingAreaRepositoryTests
    {
        private Mock<ISqlPolicyFactory> _policyFactory;
        private Mock<ISqlConnectionFactory> _connectionFactory;
        private Mock<IDbConnection> _connection;

        private PublishingAreaRepository _repository;

        [TestInitialize]
        public void SetUp()
        {
            _policyFactory = new Mock<ISqlPolicyFactory>();
            _connectionFactory = new Mock<ISqlConnectionFactory>();
            _connection = new Mock<IDbConnection>();

            _connectionFactory.Setup(_ => _.CreateConnection())
                .Returns(_connection.Object);

            _policyFactory.Setup(_ => _.CreateConnectionOpenPolicy())
                .Returns(Policy.NoOp);
            _policyFactory.Setup(_ => _.CreateQueryAsyncPolicy())
                .Returns(Policy.NoOpAsync);

            _repository = new PublishingAreaRepository(_connectionFactory.Object,
                _policyFactory.Object);
        }

        [TestMethod]
        public async Task GetFundingStreamsWithDatasets()
        {
            string[] expectedFundingStreams =
            {
               NewRandomString(), NewRandomString()
            };
            
            GivenTheDapperReturnFor("sp_getFundingStreamsWithDatasets", 
                expectedFundingStreams,
                CommandType.StoredProcedure);

            IEnumerable<string> actualPublishAreaProviders = await WhenTheFundingStreamsWithDatasetsAreQueried();

            actualPublishAreaProviders
                .Should()
                .BeEquivalentTo<string>(expectedFundingStreams);    
        }

        [TestMethod]
        public async Task GeFundingStreamsWithProviderSnapshots()
        {
            string[] expectedFundingStreamIds =
            {
               NewRandomString(), NewRandomString()
            };

            GivenTheDapperReturnFor("sp_getFundingStreamsWithProviderSnapshots",
                expectedFundingStreamIds,
                CommandType.StoredProcedure);

            IEnumerable<string> actualFundingStreamIds = await WhenTheFundingStreamsWithProviderSnapshotsAreQueried();

            actualFundingStreamIds
                .Should()
                .BeEquivalentTo<string>(expectedFundingStreamIds);
        }

        [TestMethod]
        public async Task GetDatasetMetadata()
        {
            PublishingAreaDatasetMetadata[] expectedMetaData =
            {
                NewPublishingAreaDatasetMetadata(), NewPublishingAreaDatasetMetadata(), NewPublishingAreaDatasetMetadata()
            };

            string fundingStreamId = NewRandomString();
            
            GivenTheDapperReturnFor("sp_getDatasetsByFundingStream", 
                _ => _.FundingStreamId == fundingStreamId, 
                expectedMetaData,
                CommandType.StoredProcedure);

            IEnumerable<PublishingAreaDatasetMetadata> actualPublishAreaProviders = await WhenThePublishingAreaDatasetMetadataIsQueried(fundingStreamId);

            actualPublishAreaProviders
                .Should()
                .BeEquivalentTo<PublishingAreaDatasetMetadata>(expectedMetaData);
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
                expectedPublishAreaProviders,
                CommandType.StoredProcedure);

            IEnumerable<PublishingAreaProvider> actualPublishAreaProviders = await WhenTheProvidersInSnapshotAreQueried(snapShotId);

            actualPublishAreaProviders
                .Should()
                .BeEquivalentTo<PublishingAreaProvider>(expectedPublishAreaProviders);
        }
        
        [TestMethod]
        public async Task GetLocalAuthorities()
        {
            PublishingAreaOrganisation[] expectedPublishingAreaOrganisations =
            {
                NewPublishingAreaOrganisation(), NewPublishingAreaOrganisation()
            };

            int snapShotId = NewRandomNumber();
            
            GivenTheDapperReturnFor("sp_GetPaymentOrganisationDetailsBySnapshotId", 
                _ => _.ProviderSnapshotId == snapShotId &&
                _.PaymentOrganisationType == PaymentOrganisationType.LocalAuthority.ToString(), 
                expectedPublishingAreaOrganisations,
                CommandType.StoredProcedure);

            IEnumerable<PublishingAreaOrganisation> actualPublishAreaProviders = await WhenTheLocalAuthoritiesInSnapshotAreQueried(snapShotId);

            actualPublishAreaProviders
                .Should()
                .BeEquivalentTo<PublishingAreaOrganisation>(expectedPublishingAreaOrganisations);
        }
        
        [TestMethod]
        public async Task GetProviderSnapshotMetadata()
        {
            PublishingAreaProviderSnapshot expectedMetadata = NewPublishingAreaProviderSnapshot();

            int providerSnapshotId = NewRandomNumber();
            
            GivenTheSingleDapperReturnFor("sp_GetProviderSnapshotMetadata", 
                _ => _.ProviderSnapshotId == providerSnapshotId, 
                expectedMetadata);

            PublishingAreaProviderSnapshot actualMetadata = await WhenThenSnapshotMetadataIsQueried(providerSnapshotId);

            actualMetadata
                .Should()
                .BeEquivalentTo(expectedMetadata);
        }

        [TestMethod]
        public async Task PopulateFundingPeriod()
        {
            int providerSnapshotId = NewRandomNumber();

            GivenTheSingleDapperReturnFor<string>("sp_PopulateProviderSnapshotFundingPeriodBySnapshotId",
                _ => _.ProviderSnapshotId == providerSnapshotId,
                null);

            await WhenPopulateFundingPeriodIsExecuted(providerSnapshotId);
        }

        [TestMethod]
        public async Task PopulateFundingPeriods()
        {
            GivenTheDapperReturnFor<string>("sp_PopulateAllProviderSnapshotFundingPeriod",
                null,
                CommandType.StoredProcedure);

            await WhenPopulateFundingPeriodsIsExecuted();
        }

        [TestMethod]
        public async Task GetAllOrganisations()
        {
            PublishingAreaOrganisation[] expectedPublishingAreaOrganisations =
            {
                NewPublishingAreaOrganisation(), NewPublishingAreaOrganisation()
            };

            int snapShotId = NewRandomNumber();
            
            GivenTheDapperReturnFor("sp_GetAllPaymentOrganisationsBySnapshotId", 
                _ => _.ProviderSnapshotId == snapShotId, 
                expectedPublishingAreaOrganisations,
                CommandType.StoredProcedure);

            IEnumerable<PublishingAreaOrganisation> actualPublishAreaProviders = await WhenThePaymentOrganisationsInSnapshotAreQueried(snapShotId);

            actualPublishAreaProviders
                .Should()
                .BeEquivalentTo<PublishingAreaOrganisation>(expectedPublishingAreaOrganisations);
        }
        
        [TestMethod]
        public async Task GetProviderSnapshots()
        {
            PublishingAreaProviderSnapshot[] expectedPublishAreaProviders =
            {
                NewPublishingAreaProviderSnapshot(), NewPublishingAreaProviderSnapshot(), NewPublishingAreaProviderSnapshot()
            };

            string fundingStreamId = NewRandomString();
            
            GivenTheDapperReturnFor("sp_getProviderSnapshotsByFundingStream", 
                _ => _.ProviderSnapshotId == fundingStreamId, 
                expectedPublishAreaProviders,
                CommandType.StoredProcedure);

            IEnumerable<PublishingAreaProviderSnapshot> actualPublishAreaProviders = await WhenTheSnapshotsAreQueried(fundingStreamId);

            actualPublishAreaProviders
                .Should()
                .BeEquivalentTo<PublishingAreaProviderSnapshot>(expectedPublishAreaProviders);
        }

        [TestMethod]
        public async Task GetLatestProviderSnapshotsForAllFundingStreams()
        {
            PublishingAreaProviderSnapshot[] expectedPublishAreaProviders =
            {
                NewPublishingAreaProviderSnapshot(), NewPublishingAreaProviderSnapshot(), NewPublishingAreaProviderSnapshot()
            };

            GivenTheDapperReturnFor("sp_getLatestProviderSnapshotsForAllFundingStreams",
                expectedPublishAreaProviders,
                CommandType.StoredProcedure);

            IEnumerable<PublishingAreaProviderSnapshot> actualPublishAreaProviders = await WhenTheLatestSnapshotsAreQueried();

            actualPublishAreaProviders
                .Should()
                .BeEquivalentTo<PublishingAreaProviderSnapshot>(expectedPublishAreaProviders);
        }

        [TestMethod]
        public async Task GetProviderInSnapshot()
        {
            PublishingAreaProvider expectedPublishAreaProvider = NewPublishingAreaProvider();
            
            int snapShotId = NewRandomNumber();
            string providerId = NewRandomString();
            
            GivenTheSingleDapperReturnFor("sp_getProviderDetailsByProviderId", 
                _ => _.ProviderSnapshotId == snapShotId &&
                _.ProviderId == providerId, 
                expectedPublishAreaProvider);

            PublishingAreaProvider actualPublishAreaProviders = await WhenTheProviderInSnapshotIsQueried(snapShotId, providerId);

            actualPublishAreaProviders
                .Should()
                .BeEquivalentTo(expectedPublishAreaProvider);
        }

        [TestMethod]
        public async Task GetTableNameForDataset()
        {
            string code = NewRandomString();
            int version = NewRandomNumber();

            string expectedTableName = NewRandomString();
            
            GivenTheSingleDapperReturnFor("sp_getDatasetByCode",
                _ => _.DatasetCode == code &&
                     _.Version == version,
                expectedTableName);

            string actualTableName = await WhenTheTableNameIsQueried(code, version);

            actualTableName
                .Should()
                .Be(expectedTableName);
        }
        
        [TestMethod]
        public async Task GetDataForTable()
        {
            string tableName = NewRandomString();

            DataRow[] expectedRows = new[]
            {
                NewRandomRow(), NewRandomRow(), NewRandomRow(),
            };
            
            GivenTheDapperReturnFor($"SELECT * FROM {tableName}",
                expectedRows,
                CommandType.Text);

            object[] actualData = (object[])await WhenTheDataForTableIsQueried(tableName);

            IEnumerable<string> actualDataLiterals = actualData.Select(_ => _.ToString());
            
            actualDataLiterals
                .Should()
                .BeEquivalentTo(expectedRows.Select(_ => $"{{DapperRow, One = '{_.One}', Two = '{_.Two}'}}"));
        }

        private async Task<object> WhenTheDataForTableIsQueried(string tableName)
            => await _repository.GetDataForTable(tableName);

        private async Task<string> WhenTheTableNameIsQueried(string code,
            int version)
            => await _repository.GetTableNameForDataset(code, version);
        
        private async Task<IEnumerable<PublishingAreaOrganisation>> WhenThePaymentOrganisationsInSnapshotAreQueried(int snapshotId)
            => await _repository.GetAllOrganisations(snapshotId);
        
        private async Task<IEnumerable<PublishingAreaOrganisation>> WhenTheLocalAuthoritiesInSnapshotAreQueried(int snapshotId)
            => await _repository.GetLocalAuthorities(snapshotId);
        
        private async Task<IEnumerable<PublishingAreaProvider>> WhenTheProvidersInSnapshotAreQueried(int snapshotId)
            => await _repository.GetProvidersInSnapshot(snapshotId);
        
        private async Task<IEnumerable<PublishingAreaProviderSnapshot>> WhenTheSnapshotsAreQueried(string fundingStreamId)
            => await _repository.GetProviderSnapshots(fundingStreamId);

        private async Task<IEnumerable<PublishingAreaProviderSnapshot>> WhenTheLatestSnapshotsAreQueried()
           => await _repository.GetLatestProviderSnapshotsForAllFundingStreams();

        private async Task<PublishingAreaProviderSnapshot> WhenThenSnapshotMetadataIsQueried(int providerSnapshotId)
            => await _repository.GetProviderSnapshotMetadata(providerSnapshotId);
        
        private async Task<PublishingAreaProvider> WhenTheProviderInSnapshotIsQueried(int snapshotId, 
            string providerId)
            => await _repository.GetProviderInSnapshot(snapshotId, providerId);

        private async Task<IEnumerable<PublishingAreaDatasetMetadata>> WhenThePublishingAreaDatasetMetadataIsQueried(string fundingStreamId)
            => await _repository.GetDatasetMetadata(fundingStreamId);

        private async Task WhenPopulateFundingPeriodsIsExecuted()
            => await _repository.PopulateFundingPeriods();

        private async Task WhenPopulateFundingPeriodIsExecuted(int providerSnapshotId)
            => await _repository.PopulateFundingPeriod(providerSnapshotId);

        private async Task<IEnumerable<string>> WhenTheFundingStreamsWithDatasetsAreQueried()
            => await _repository.GetFundingStreamsWithDatasets();

        private async Task<IEnumerable<string>> WhenTheFundingStreamsWithProviderSnapshotsAreQueried()
            => await _repository.GetFundingStreamsWithProviderSnapshots();

        private void GivenTheDapperReturnFor<TReturn>(string sql,
            IEnumerable<TReturn> items,
            CommandType commandType) 
        {
            GivenTheDapperReturnFor(sql, _ => true, items, commandType);
        }

        private void GivenTheDapperReturnFor<TReturn>(string sql,
            Func<dynamic, bool> parameterConstraint,
            IEnumerable<TReturn> items,
            CommandType commandType)
        {
            _connection.SetupDapperAsync(_ => _.QueryAsync<TReturn>(sql,
                    It.Is<object>(prm => parameterConstraint(prm)),
                    null,
                    null,
                    commandType))
                .ReturnsAsync(items);
        }
        
        private void GivenTheSingleDapperReturnFor<TReturn>(string sql,
            Func<dynamic, bool> parameterConstraint,
           TReturn item)
        {
            _connection.SetupDapperAsync(_ => _.QuerySingleOrDefaultAsync<TReturn>(sql,
                    It.Is<object>(prm => parameterConstraint(prm)),
                    null,
                    null,
                    CommandType.StoredProcedure))
                .ReturnsAsync(item);
        }
        
        private int NewRandomNumber() => new RandomNumberBetween(1, int.MaxValue);
        
        private string NewRandomString() => new RandomString();

        private PublishingAreaProvider NewPublishingAreaProvider(Action<PublishingAreaProviderBuilder> setUp = null)
        {
            PublishingAreaProviderBuilder publishingAreaProviderBuilder = new PublishingAreaProviderBuilder();

            setUp?.Invoke(publishingAreaProviderBuilder);

            return publishingAreaProviderBuilder.Build();
        }

        private PublishingAreaProviderSnapshot NewPublishingAreaProviderSnapshot(Action<PublishingAreaProviderSnapshotBuilder> setUp = null)
        {
            PublishingAreaProviderSnapshotBuilder publishingAreaProviderSnapshotBuilder = new PublishingAreaProviderSnapshotBuilder();

            setUp?.Invoke(publishingAreaProviderSnapshotBuilder);
            
            return publishingAreaProviderSnapshotBuilder.Build();
        }

        private PublishingAreaDatasetMetadata NewPublishingAreaDatasetMetadata(Action<PublishingAreaDatasetMetadataBuilder> setUp = null)
        {
            PublishingAreaDatasetMetadataBuilder publishingAreaDatasetMetadataBuilder = new PublishingAreaDatasetMetadataBuilder();

            setUp?.Invoke(publishingAreaDatasetMetadataBuilder);
            
            return publishingAreaDatasetMetadataBuilder.Build();
        }
        
        private PublishingAreaOrganisation NewPublishingAreaOrganisation(Action<PublishingAreaOrganisationBuilder> setUp = null)
        {
            PublishingAreaOrganisationBuilder publishingAreaOrganisationBuilder = new PublishingAreaOrganisationBuilder();

            setUp?.Invoke(publishingAreaOrganisationBuilder);
            
            return publishingAreaOrganisationBuilder.Build();
        }
        
        private DataRow NewRandomRow() => new DataRow
        {
            One = NewRandomNumber(),
            Two = NewRandomString()
        };

        public class DataRow
        {
            public int One { get; set; }
            
            public string Two { get; set; }
        }
    }
}