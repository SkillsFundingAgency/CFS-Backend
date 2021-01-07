using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.CalcEngine.UnitTests;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Calculator
{
    [TestClass]
    public class ProviderSourceDatasetsRepositoryTests
    {
        private IProviderSourceDatasetVersionKeyProvider _versionKeyProvider;
        private IFileSystemCache _fileSystemCache;
        private ICosmosRepository _cosmosRepository;
        private ICalculatorResiliencePolicies _resiliencePolicies;
        private ProviderSourceDatasetsRepository _repository;

        private static readonly List<string> _emptyListOfIds = new List<string>();

        private string _specificationId;

        [TestInitialize]
        public void SetUp()
        {
            _versionKeyProvider = Substitute.For<IProviderSourceDatasetVersionKeyProvider>();
            _fileSystemCache = Substitute.For<IFileSystemCache>();
            _cosmosRepository = Substitute.For<ICosmosRepository>();
            _resiliencePolicies = CalcEngineResilienceTestHelper.GenerateTestPolicies();

            _repository = new ProviderSourceDatasetsRepository(_cosmosRepository, _resiliencePolicies);

            _specificationId = "specId";
        }

        [TestMethod]
        public async Task ReturnsCorrectDatasets_GivenValidSetOfProviders()
        {
            string relationshipIdOne = NewRandomString();
            string relationshipIdTwo = NewRandomString();

            Guid relationshipOneVersionKey = Guid.NewGuid();

            ProviderSourceDataset dataSetOne = NewProviderSourceDataset();
            ProviderSourceDataset dataSetTwo = NewProviderSourceDataset();
            ProviderSourceDataset dataSetThree = NewProviderSourceDataset();
            ProviderSourceDataset dataSetFour = NewProviderSourceDataset();
            ProviderSourceDataset dataSetFive = NewProviderSourceDataset();
            ProviderSourceDataset dataSetSix = NewProviderSourceDataset();
            ProviderSourceDataset dataSetSeven = NewProviderSourceDataset();
            ProviderSourceDataset dataSetEight = NewProviderSourceDataset();

            GivenTheDatasetVersionKey(relationshipIdOne, relationshipOneVersionKey);
            AndTheProviderSourceDataset(_specificationId, dataSetOne.ProviderId, relationshipIdTwo, dataSetOne);
            AndTheProviderSourceDataset(_specificationId, dataSetTwo.ProviderId, relationshipIdTwo, dataSetTwo);
            AndTheProviderSourceDataset(_specificationId, dataSetThree.ProviderId, relationshipIdTwo, dataSetThree);
            AndTheProviderSourceDataset(_specificationId, dataSetFour.ProviderId, relationshipIdTwo, dataSetFour);
            AndTheProviderSourceDataset(_specificationId, dataSetFive.ProviderId, relationshipIdTwo, dataSetFive);
            AndTheProviderSourceDataset(_specificationId, dataSetSix.ProviderId, relationshipIdTwo, dataSetSix);
            AndTheProviderSourceDataset(_specificationId, dataSetSeven.ProviderId, relationshipIdTwo, dataSetSeven);
            AndTheProviderSourceDataset(_specificationId, dataSetEight.ProviderId, relationshipIdTwo, dataSetEight);

            Dictionary<string, Dictionary<string, ProviderSourceDataset>> datasets =
                await _repository.GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(
                    _specificationId,
                    new[]
                    {
                        dataSetOne.ProviderId,
                        dataSetTwo.ProviderId,
                        dataSetThree.ProviderId,
                        dataSetFour.ProviderId,
                        dataSetFive.ProviderId,
                        dataSetSix.ProviderId,
                        dataSetSeven.ProviderId,
                        dataSetEight.ProviderId
                    },
                    new[]
                    {
                        relationshipIdOne,
                        relationshipIdTwo
                    });

            datasets.Values.SelectMany(x => x.Values).Count()
                .Should()
                .Be(8);

            datasets.Values.SelectMany(x => x.Values).Select(_ => _.Id)
                .Should()
                .BeEquivalentTo(new[]
                {
                    dataSetOne,
                    dataSetTwo,
                    dataSetThree,
                    dataSetFour,
                    dataSetFive,
                    dataSetSix,
                    dataSetSeven,
                    dataSetEight
                }.Select(_ => _.Id));
        }

        [DataTestMethod]
        [DataRow(null, null)]
        [DataRow("",  null)]
        [DataRow(null,  "")]
        [DataRow("",  "")]
        public async Task ReturnsEmptyDatasets_GivenEmptyProvidersOrDataRelationshipIds(string providerIds, string dataRelationshipIds)
        {
            Dictionary<string, Dictionary<string, ProviderSourceDataset>> datasets =
                await _repository.GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(
                    _specificationId,
                    providerIds == null? null : new List<string>(),
                    dataRelationshipIds == null? null : new List<string>());

            datasets.Should().BeEmpty();
        }

        private void GivenTheDatasetVersionKey(string relationshipId, Guid versionKey)
        {
            _versionKeyProvider.GetProviderSourceDatasetVersionKey(relationshipId)
                .Returns(versionKey);
        }

        private void AndTheProviderSourceDataset(
            string specificationId,
            string providerId,
            string dataRelationshipId,
            ProviderSourceDataset providerSourceDataset)
        {
            string documentKey = $"{specificationId}_{dataRelationshipId}_{providerId}";

            _cosmosRepository
                .TryReadDocumentByIdPartitionedAsync<ProviderSourceDataset>(Arg.Is(documentKey), Arg.Is(providerId))
                .Returns(new DocumentEntity<ProviderSourceDataset>
                {
                    Deleted = false,
                    Content = providerSourceDataset
                });
        }

        private string NewRandomString() => new RandomString();

        public ProviderSourceDataset NewProviderSourceDataset() =>
            new ProviderSourceDatasetBuilder()
                .Build();
    }
}