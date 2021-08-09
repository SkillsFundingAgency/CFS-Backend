using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces.Helpers;
using CalculateFunding.Services.Datasets.Converter;
using CalculateFunding.Tests.Common.Builders;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Datasets.Services.Converter
{
    [TestClass]
    public class DatasetIndexerTests
    {
        private Mock<ISearchRepository<DatasetIndex>> _datasets;
        private Mock<ISearchRepository<DatasetVersionIndex>> _datasetVersions;

        private DatasetIndexer _indexer;
        
        [TestInitialize]
        public void SetUp()
        {
            _datasets = new Mock<ISearchRepository<DatasetIndex>>();
            _datasetVersions = new Mock<ISearchRepository<DatasetVersionIndex>>();

            _indexer = new DatasetIndexer(_datasetVersions.Object,
                _datasets.Object,
                new DatasetsResiliencePolicies
                {
                    DatasetSearchService = Policy.NoOpAsync(),
                    DatasetVersionSearchService = Policy.NoOpAsync()
                },
                Logger.None);
        }

        [TestMethod]
        public async Task IndexesDatasetAndCurrentFromSuppliedDataset()
        {
            Dataset dataset = NewDataset(_ => _.WithCurrent(NewDatasetVersion()));

            await WhenTheDatasetAndVersionIsIndexed(dataset);
            
            ThenTheDatasetWasIndexed(dataset);
            AndTheDatasetsVersionWasIndexed(dataset);
        }

        [TestMethod]
        public void ThrowsExceptionWithIndexingErrorsWhenReturnedFromDatasetIndexing()
        {
            IndexError[] expectedErrors = new[]
            {
                NewIndexError(),
                NewIndexError(),
                NewIndexError()
            };
            
            GivenTheIndexingErrorsForDatasets(expectedErrors);

            Func<Task> invocation = () => WhenTheDatasetAndVersionIsIndexed(NewDataset(_ => 
                _.WithCurrent(NewDatasetVersion())));

            invocation
                .Should()
                .ThrowAsync<InvalidOperationException>()
                .Result
                .Which
                .Message
                .Should()
                .Be($"Could not complete datasetIndexing. {expectedErrors.Select(_ => $"{_.Key} - {_.ErrorMessage}").Join("; ")}");
        }
        
        [TestMethod]
        public void ThrowsExceptionWithIndexingErrorsWhenReturnedFromDatasetVersionsIndexing()
        {
            IndexError[] expectedErrors = new[]
            {
                NewIndexError(),
                NewIndexError(),
                NewIndexError()
            };
            
            GivenTheIndexingErrorsForDatasetVersions(expectedErrors);

            Func<Task> invocation = () => WhenTheDatasetAndVersionIsIndexed(NewDataset(_ => 
                _.WithCurrent(NewDatasetVersion())));

            invocation
                .Should()
                .ThrowAsync<InvalidOperationException>()
                .Result
                .Which
                .Message
                .Should()
                .Be($"Could not complete datasetVersionIndexing. {expectedErrors.Select(_ => $"{_.Key} - {_.ErrorMessage}").Join("; ")}");     
        }

        private void GivenTheIndexingErrorsForDatasets(params IndexError[] errors)
            => _datasets.Setup(_ => _.Index(It.IsAny<IEnumerable<DatasetIndex>>()))
                .ReturnsAsync(errors);
        
        private void GivenTheIndexingErrorsForDatasetVersions(params IndexError[] errors)
            => _datasetVersions.Setup(_ => _.Index(It.IsAny<IEnumerable<DatasetVersionIndex>>()))
                .ReturnsAsync(errors);


        private async Task WhenTheDatasetAndVersionIsIndexed(Dataset dataset)
            => await _indexer.IndexDatasetAndVersion(dataset);

        private void ThenTheDatasetWasIndexed(Dataset dataset)
        {
            DatasetVersion datasetVersion = dataset.Current;
            
            _datasets.Verify(_ => _.Index(It.Is<IEnumerable<DatasetIndex>>(indexes =>
                ConstraintHelpers.AreEquivalent(indexes.SingleOrDefault(), new DatasetIndex
                {
                    Id = dataset.Id,
                    Name = dataset.Name,
                    DefinitionId = dataset.Definition.Id,
                    DefinitionName = dataset.Definition.Name,
                    Status = datasetVersion.PublishStatus.ToString(),
                    LastUpdatedDate = datasetVersion.Date,
                    Description = datasetVersion.Description,
                    Version = datasetVersion.Version,
                    ChangeNote = datasetVersion.Comment,
                    LastUpdatedById = datasetVersion.Author.Id,
                    LastUpdatedByName = datasetVersion.Author.Name,
                    FundingStreamId = datasetVersion.FundingStream.Id,
                    FundingStreamName = datasetVersion.FundingStream.Name
                }))));  
        }

        private void AndTheDatasetsVersionWasIndexed(Dataset dataset)
        {
            DatasetVersion datasetVersion = dataset.Current;
            
            _datasetVersions.Verify(_ => _.Index(It.Is<IEnumerable<DatasetVersionIndex>>(indexes =>
                ConstraintHelpers.AreEquivalent(indexes.SingleOrDefault(), new DatasetVersionIndex
                {
                    Id = $"{dataset.Id}-{datasetVersion.Version}",
                    DatasetId = dataset.Id,
                    Name = dataset.Name,
                    Version = datasetVersion.Version,
                    BlobName = datasetVersion.BlobName,
                    ChangeNote = datasetVersion.Comment,
                    ChangeType = datasetVersion.ChangeType.ToString(),
                    DefinitionName = dataset.Definition.Name,
                    Description = datasetVersion.Description,
                    LastUpdatedDate = datasetVersion.Date,
                    LastUpdatedByName = datasetVersion.Author.Name,
                    FundingStreamId = datasetVersion.FundingStream.Id,
                    FundingStreamName = datasetVersion.FundingStream.Name
                }))));       
        }
        
        private Dataset NewDataset(Action<DatasetBuilder> setUp = null)
        {
            DatasetBuilder datasetBuilder = new DatasetBuilder()
                .WithDefinition(NewDatasetDefinitionVersion());

            setUp?.Invoke(datasetBuilder);
            
            return datasetBuilder.Build();
        }

        private DatasetVersion NewDatasetVersion(Action<DatasetVersionBuilder> setUp = null)
        {
            DatasetVersionBuilder datasetVersionBuilder = new DatasetVersionBuilder()
                .WithAuthor(NewReference())
                .WithFundingStream(NewReference());

            setUp?.Invoke(datasetVersionBuilder);
            
            return datasetVersionBuilder.Build();
        }

        private DatasetDefinitionVersion NewDatasetDefinitionVersion() => new DatasetDefinitionVersionBuilder().Build();

        private Reference NewReference() => new ReferenceBuilder().Build();

        private IndexError NewIndexError() => new IndexErrorBuilder().Build();
    }
}