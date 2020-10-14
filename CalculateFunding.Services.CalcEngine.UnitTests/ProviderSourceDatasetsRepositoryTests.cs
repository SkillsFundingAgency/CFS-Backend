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
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Options;
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

        private ProviderSourceDatasetsRepository _repository;

        private string _specificationId;

        [TestInitialize]
        public void SetUp()
        {
            _versionKeyProvider = Substitute.For<IProviderSourceDatasetVersionKeyProvider>();
            _fileSystemCache = Substitute.For<IFileSystemCache>();
            _cosmosRepository = Substitute.For<ICosmosRepository>();

            _repository = new ProviderSourceDatasetsRepository(_cosmosRepository,
                new EngineSettings
                {
                    GetProviderSourceDatasetsDegreeOfParallelism = 1
                },
                _versionKeyProvider,
                _fileSystemCache);

            _specificationId = "specId";
        }

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
            AndTheCachedProviderSourceDataSet(relationshipIdOne, providerIdOne, relationshipOneVersionKey, dataSetOne);
            AndTheCachedProviderSourceDataSet(relationshipIdOne, providerIdTwo, relationshipOneVersionKey, dataSetTwo);
            AndTheCachedProviderSourceDataSet(relationshipIdOne, providerIdThree, relationshipOneVersionKey, dataSetThree);
            AndTheCachedProviderSourceDataSet(relationshipIdOne, providerIdFour, relationshipOneVersionKey, dataSetFour);
            AndTheProviderSourceDataset(_specificationId, providerIdOne, relationshipIdTwo, dataSetFive);
            AndTheProviderSourceDataset(_specificationId, providerIdTwo, relationshipIdTwo, dataSetSix);
            AndTheProviderSourceDataset(_specificationId, providerIdThree, relationshipIdTwo, dataSetSeven);
            AndTheProviderSourceDataset(_specificationId, providerIdFour, relationshipIdTwo, dataSetEight);

            IDictionary<string, IEnumerable<ProviderSourceDataset>> datasets = await _repository.GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(
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

            datasets.SelectMany(x => x.Value).Count()
                .Should()
                .Be(8);

            datasets.SelectMany(x => x.Value).Select(_ => _.Id)
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
                .ReadDocumentByIdPartitionedAsync<ProviderSourceDataset>(Arg.Is(documentKey), Arg.Is(providerId))
                .Returns(new DocumentEntity<ProviderSourceDataset>
                { 
                    Deleted = false, 
                    Content = providerSourceDataset 
                });
        }

        private void AndTheCachedProviderSourceDataSet(string relationshipId,
            string providerId,
            Guid versionKey,
            ProviderSourceDataset providerSourceDataset)
        {
            string key = $"{relationshipId}_{providerId}_{versionKey}";

            _fileSystemCache.Exists(Arg.Is<FileSystemCacheKey>(_ =>
                    _.Key.Equals(key)))
                .Returns(true);

            byte[] buffer = providerSourceDataset.AsJson().AsUTF8Bytes();

            _fileSystemCache.Get(Arg.Is<FileSystemCacheKey>(_ =>
                    _.Key.Equals(key)))
                .Returns(new MemoryStream(buffer, 0, buffer.Length, false, true));
        }

        private void AndTheProviderSourceDatasetWasCachedToTheFileSystem(ProviderSourceDataset providerSourceDataset)
        {
            ThenTheProviderSourceDatasetWasCachedToTheFileSystem(providerSourceDataset);
        }

        private void ThenTheProviderSourceDatasetWasCachedToTheFileSystem(ProviderSourceDataset providerSourceDataset)
        {
            _fileSystemCache
                .Received(1)
                .Add(Arg.Any<FileSystemCacheKey>(),
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