using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.CalcEngine.UnitTests;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ReceivedExtensions;

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
        [Ignore("Filesystem caching has been removed, but the test suite needs updating for cosmos.")]
        [TestMethod]
        public async Task UsesFileSystemCaching()
        {
            string providerIdOne = NewRandomString();
            string providerIdTwo = NewRandomString();
            string providerIdThree = NewRandomString();
            string providerIdFour = NewRandomString();

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
            AndTheProviderSourceDataset(_specificationId, providerIdOne, relationshipIdTwo, dataSetFive);
            AndTheProviderSourceDataset(_specificationId, providerIdTwo, relationshipIdTwo, dataSetSix);
            AndTheProviderSourceDataset(_specificationId, providerIdThree, relationshipIdTwo, dataSetSeven);
            AndTheProviderSourceDataset(_specificationId, providerIdFour, relationshipIdTwo, dataSetEight);

            Dictionary<string, Dictionary<string, ProviderSourceDataset>> datasets = await _repository.GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(
               _specificationId,
                new[]
                {
                    providerIdOne,
                    providerIdTwo,
                    providerIdThree,
                    providerIdFour
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

            ThenTheProviderSourceDatasetWasCachedToTheFileSystem(dataSetFive);
            AndTheProviderSourceDatasetWasCachedToTheFileSystem(dataSetSix);
            AndTheProviderSourceDatasetWasCachedToTheFileSystem(dataSetSeven);
            AndTheProviderSourceDatasetWasCachedToTheFileSystem(dataSetEight);
            await AndTheANewVersionKeyWasCachedForTheRelationshipId(relationshipIdTwo);
        }

        private async Task AndTheANewVersionKeyWasCachedForTheRelationshipId(string relationshipId)
        {
            await _versionKeyProvider
                 .Received(1)
                 .AddOrUpdateProviderSourceDatasetVersionKey(Arg.Is(relationshipId),
                     Arg.Is<Guid>(_ => _ != Guid.Empty));
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


        private void AndTheProviderSourceDatasetWasCachedToTheFileSystem(ProviderSourceDataset providerSourceDataset)
        {
            ThenTheProviderSourceDatasetWasCachedToTheFileSystem(providerSourceDataset);
        }

        private void ThenTheProviderSourceDatasetWasCachedToTheFileSystem(ProviderSourceDataset providerSourceDataset)
        {
            _fileSystemCache
                .Received(1)
                .Add(Arg.Any<ProviderSourceDatasetFileSystemCacheKey>(),
                    Arg.Is<MemoryStream>(stream => StreamMatchesDataset(stream, providerSourceDataset)),
                    Arg.Any<CancellationToken>(),
                    Arg.Is(true));
        }

        private bool StreamMatchesDataset(MemoryStream stream, ProviderSourceDataset dataset)
        {
            using (MemoryStream copy = new MemoryStream(stream.GetBuffer()))
            {
                string actualJson = copy.AsPoco<ProviderSourceDataset>().AsJson();
                string expectedJson = dataset.AsJson();

                return actualJson.Equals(expectedJson);
            }
        }

        private string NewRandomString()
        {
            return new RandomString();
        }

        public ProviderSourceDataset NewProviderSourceDataset()
        {
            return new ProviderSourceDatasetBuilder()
                .Build();
        }
    }
}