using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Datasets;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class ProviderSourceDatasetBulkRepositoryTests
    {
        private Mock<ICosmosRepository> _cosmos;

        private ProviderSourceDatasetBulkRepository _repository;

        [TestInitialize]
        public void SetUp()
        {
            _cosmos = new Mock<ICosmosRepository>();

            _cosmos.Setup(_ => _.UpsertAsync(It.IsAny<ProviderSourceDataset>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>()))
                .ReturnsAsync(HttpStatusCode.OK);

            _cosmos.Setup(_ => _.DeleteAsync<ProviderSourceDataset>(It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>()))
                .ReturnsAsync(HttpStatusCode.OK);

            _cosmos.Setup(_ => _.CreateAsync(It.IsAny<KeyValuePair<string, ProviderSourceDatasetHistory>>()))
                .ReturnsAsync(HttpStatusCode.OK);


            _repository = new ProviderSourceDatasetBulkRepository(_cosmos.Object);
        }

        [TestMethod]
        public void DeleteCurrentProviderSourceDatasetsGuardsAgainstMissingDatasets()
        {
            Action invocation = () => WhenTheCurrentProviderSourceDatasetsAreDeleted(null)
                .GetAwaiter()
                .GetResult();

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("providerSourceDatasets");
        }

        [TestMethod, Ignore("reverted to old api as throws bad request at runtime")]
        public async Task DeleteCurrentProviderSourceDatasetsFloodsCosmosAndWaitsForAllTasks()
        {
            ProviderSourceDataset one = NewProviderSourceDataset();
            ProviderSourceDataset two = NewProviderSourceDataset();
            ProviderSourceDataset three = NewProviderSourceDataset();
            ProviderSourceDataset four = NewProviderSourceDataset();

            await WhenTheCurrentProviderSourceDatasetsAreDeleted(one, two, three, four);

            ThenTheProviderSourceDatasetsWereDeleted(one, two, three, four);
        }

        [TestMethod]
        public void UpdateCurrentProviderSourceDatasetsGuardsAgainstMissingDatasets()
        {
            Action invocation = () => WhenTheCurrentProviderSourceDatasetsAreUpdated(null)
                .GetAwaiter()
                .GetResult();

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("providerSourceDatasets");
        }

        [TestMethod]
        public async Task UpdateCurrentProviderSourceDatasetsFloodsCosmosAndWaitsForAllTasks()
        {
            ProviderSourceDataset one = NewProviderSourceDataset();
            ProviderSourceDataset two = NewProviderSourceDataset();
            ProviderSourceDataset three = NewProviderSourceDataset();

            await WhenTheCurrentProviderSourceDatasetsAreUpdated(one, two, three);

            ThenTheProviderSourceDatasetsWereUpserted(one, two, three);
        }

        [TestMethod]
        public void UpdateProviderSourceDatasetHistoryGuardsAgainstMissingDatasets()
        {
            Action invocation = () => WhenTheCurrentProviderSourceDatasetsHistoriesAreUpdated(null)
                .GetAwaiter()
                .GetResult();

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("providerSourceDatasets");
        }

        [TestMethod, Ignore("had to roll this back to the old bulk impl the framework bulk option was causing stack overflows in testing")]
        public async Task UpdateProviderSourceDatasetHistoryFloodsCosmosAndWaitsForAllTasks()
        {
            ProviderSourceDatasetHistory one = NewProviderSourceDatasetHistory();
            ProviderSourceDatasetHistory two = NewProviderSourceDatasetHistory();
            ProviderSourceDatasetHistory three = NewProviderSourceDatasetHistory();
            ProviderSourceDatasetHistory four = NewProviderSourceDatasetHistory();
            ProviderSourceDatasetHistory five = NewProviderSourceDatasetHistory();

            await WhenTheCurrentProviderSourceDatasetsHistoriesAreUpdated(one, two, three, four, five);

            ThenTheProviderSourceDatasetHistoriesWereCreated(one, two, three, four, five);
        }

        private void ThenTheProviderSourceDatasetsWereDeleted(params ProviderSourceDataset[] providerSourceDatasets)
        {
            foreach (ProviderSourceDataset providerSourceDataset in providerSourceDatasets)
            {
                _cosmos.Verify(_ => _.DeleteAsync<ProviderSourceDataset>(providerSourceDataset.Id, providerSourceDataset.ProviderId, false, null),
                    Times.Once);
            }
        }

        private void ThenTheProviderSourceDatasetsWereUpserted(params ProviderSourceDataset[] providerSourceDatasets)
        {
            foreach (ProviderSourceDataset providerSourceDataset in providerSourceDatasets)
            {
                _cosmos.Verify(_ => _.UpsertAsync(providerSourceDataset, providerSourceDataset.ProviderId, true, true, null),
                    Times.Once);
            }
        }

        private void ThenTheProviderSourceDatasetHistoriesWereCreated(params ProviderSourceDatasetHistory[] providerSourceDatasets)
        {
            foreach (ProviderSourceDatasetHistory providerSourceDataset in providerSourceDatasets)
            {
                _cosmos.Verify(_ => _.CreateAsync(It.Is<KeyValuePair<string, ProviderSourceDatasetHistory>>(kvp =>
                        kvp.Key == providerSourceDataset.ProviderId &&
                        ReferenceEquals(kvp.Value, providerSourceDataset))),
                    Times.Once);
            }
        }

        private async Task WhenTheCurrentProviderSourceDatasetsAreDeleted(params ProviderSourceDataset[] providerSourceDatasets)
            => await _repository.DeleteCurrentProviderSourceDatasets(providerSourceDatasets);

        private async Task WhenTheCurrentProviderSourceDatasetsAreUpdated(params ProviderSourceDataset[] providerSourceDatasets)
            => await _repository.UpdateCurrentProviderSourceDatasets(providerSourceDatasets);

        private async Task WhenTheCurrentProviderSourceDatasetsHistoriesAreUpdated(params ProviderSourceDatasetHistory[] providerSourceDatasets)
            => await _repository.UpdateProviderSourceDatasetHistory(providerSourceDatasets);

        private ProviderSourceDataset NewProviderSourceDataset()
            => new ProviderSourceDatasetBuilder().Build();

        private ProviderSourceDatasetHistory NewProviderSourceDatasetHistory()
            => new ProviderSourceDatasetHistoryBuilder().Build();
    }
}