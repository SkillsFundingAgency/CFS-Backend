using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Converter;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Datasets.Services.Converter
{
    [TestClass]
    public class DatasetCloneBuilderTests
    {
        private Mock<IBlobClient> _blobs;
        private Mock<IExcelDatasetReader> _reader;
        private Mock<IExcelDatasetWriter> _writer;
        private Mock<IDatasetIndexer> _indexer;
        private Mock<IDatasetRepository> _datasets;

        private byte[] _lastUploadedBlobData;

        private DatasetCloneBuilder _builder;

        [TestInitialize]
        public void SetUp()
        {
            _blobs = new Mock<IBlobClient>();
            _reader = new Mock<IExcelDatasetReader>();
            _indexer = new Mock<IDatasetIndexer>();
            _writer = new Mock<IExcelDatasetWriter>();
            _datasets = new Mock<IDatasetRepository>();

            _builder = new DatasetCloneBuilder(_blobs.Object,
                _datasets.Object,
                _reader.Object,
                _writer.Object,
                _indexer.Object,
                new DatasetsResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync(),
                    DatasetRepository = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public void LoadOriginalDatasetFailsWhenNoDatasetSupplied()
        {
            Func<Task> invocation = () => WhenTheOriginalDatasetIsLoaded(null, NewDatasetDefinition());

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .Result
                .Which
                .Message
                .Should()
                .Be("No dataset supplied to load excel blob data from.");
        }

        [TestMethod]
        public void LoadOriginalDatasetFailsWhenNoDatasetDefinitionSupplied()
        {
            Func<Task> invocation = () => WhenTheOriginalDatasetIsLoaded(NewDataset(), null);

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .Result
                .Which
                .Message
                .Should()
                .Be("No dataset definition supplied to load excel blob data from.");
        }

        [TestMethod]
        public void LoadOriginalDatasetFailsWhenNoLocatedForSuppliedDatasetDetails()
        {
            Func<Task> invocation = () => WhenTheOriginalDatasetIsLoaded(NewDataset(), NewDatasetDefinition());

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .Result
                .Which
                .Message
                .Should()
                .StartWith("No blob located with path ");
        }

        [TestMethod]
        public void LoadOriginalDatasetFailsWhenExcelStreamForSuppliedDatasetDetailsHasNoData()
        {
            Dataset dataset = NewDataset();
            DatasetDefinition datasetDefinition = NewDatasetDefinition();

            ICloudBlob blob = NewBlob().Object;

            GivenTheBlob(dataset.Current.BlobName, blob);
            AndTheBlobStream(blob, new MemoryStream());

            Func<Task> invocation = () => WhenTheOriginalDatasetIsLoaded(dataset, datasetDefinition);

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .Result
                .Which
                .Message
                .Should()
                .EndWith(" contains no data.");
        }

        [TestMethod]
        public async Task LoadOriginalDatasetReadsExcelDataFromBlobIndicatedFromSuppliedDatasetDetails()
        {
            Dataset dataset = NewDataset();
            DatasetDefinition datasetDefinition = NewDatasetDefinition();

            ICloudBlob blob = NewBlob().Object;

            GivenTheBlob(dataset.Current.BlobName, blob);

            MemoryStream excelStream = new MemoryStream(new byte[10]);

            AndTheBlobStream(blob, excelStream);

            IEnumerable<TableLoadResult> expectedData = new[]
            {
                NewTableLoadResult(),
                NewTableLoadResult(),
                NewTableLoadResult(),
                NewTableLoadResult()
            };

            AndTheTableLoadResults(excelStream, datasetDefinition, expectedData);

            await WhenTheOriginalDatasetIsLoaded(dataset, datasetDefinition);

            _builder
                .DatasetData
                .Should()
                .BeEquivalentTo(expectedData);
        }

        [TestMethod]
        public void GetExistingIdentifierValuesQueriesTheDatasetForTheSuppliedFieldNameValue()
        {
            string fieldName = NewRandomString();

            string valueOne = NewRandomString();
            string valueTwo = NewRandomString();
            string valueThree = NewRandomString();

            TableLoadResult datasetTable = NewTableLoadResult(_ =>
                _.WithRows(NewRowLoadResult(row => row.WithFields(NewField(),
                        NewField(fieldName, valueOne),
                        NewField())),
                    NewRowLoadResult(row => row.WithFields(NewField(),
                        NewField(fieldName, valueTwo),
                        NewField())),
                    NewRowLoadResult(row => row.WithFields(NewField(),
                        NewField(fieldName, valueThree),
                        NewField()))));

            GivenTheDatasetData(datasetTable);

            IEnumerable<string> existingIdentifierValues = WhenTheExistingIdentifierValuesAreQueried(fieldName);

            existingIdentifierValues
                .Should()
                .BeEquivalentTo(valueOne, valueTwo, valueThree);
        }

        [TestMethod]
        public void CopyRowReturnsNotFoundResultIfNoMatchForSourceIdInDataset()
        {
            TableLoadResult datasetTable = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult()));

            GivenTheDatasetData(datasetTable);

            string fieldName = NewRandomString();
            string sourceProviderId = NewRandomString();
            string destinationProviderId = NewRandomString();

            RowCopyResult result = WhenTheRowIsCopied(fieldName, sourceProviderId, destinationProviderId);

            result
                .Should()
                .BeEquivalentTo(NewRowCopyResult(_ => _.WithOutcome(RowCopyOutcome.SourceRowNotFound)
                    .WithEligibleConverter(NewEligibleConverter(ec => 
                        ec.WithPreviousProviderIdentifier(sourceProviderId)
                        .WithProviderId(destinationProviderId)))));

            datasetTable.Rows
                .Count
                .Should()
                .Be(1);
        }
        
        [TestMethod]
        public void CopyRowReturnsDestinationExistsResultIfMatchForDestinationIdInTheDataset()
        {
            string fieldName = NewRandomString();
            string sourceProviderId = NewRandomString();
            string destinationProviderId = NewRandomString();

            
            TableLoadResult datasetTable = NewTableLoadResult(_ => _.WithRows(
                NewRowLoadResult(row => row.WithFields(NewField(fieldName, sourceProviderId))),
                    NewRowLoadResult(row => row.WithFields(NewField(fieldName, destinationProviderId)))));

            GivenTheDatasetData(datasetTable);

            
            RowCopyResult result = WhenTheRowIsCopied(fieldName, sourceProviderId, destinationProviderId);

            result
                .Should()
                .BeEquivalentTo(NewRowCopyResult(_ => _.WithOutcome(RowCopyOutcome.DestinationRowAlreadyExists)
                    .WithEligibleConverter(NewEligibleConverter(ec => 
                        ec.WithPreviousProviderIdentifier(sourceProviderId)
                            .WithProviderId(destinationProviderId)))));
            
            datasetTable.Rows
                .Count
                .Should()
                .Be(2);
        }
        
        [TestMethod]
        public void CopyRowCopiesSourceRowToDestinationIdentifierIdAndAddsToDatasetIfNotInDatasetYet()
        {
            string fieldName = NewRandomString();
            string sourceProviderId = NewRandomString();
            string destinationProviderId = NewRandomString();

            (string, object) fieldOne = NewField();
            (string, object) fieldTwo = NewField();

            RowLoadResult sourceRow = NewRowLoadResult(row => row.WithFields(NewField(fieldName, sourceProviderId), 
                fieldOne, 
                fieldTwo));
            
            TableLoadResult datasetTable = NewTableLoadResult(_ => _.WithRows(
                sourceRow));

            GivenTheDatasetData(datasetTable);
            
            RowCopyResult result = WhenTheRowIsCopied(fieldName, sourceProviderId, destinationProviderId);

            result
                .Should()
                .BeEquivalentTo(NewRowCopyResult(_ => _.WithOutcome(RowCopyOutcome.Copied)
                    .WithEligibleConverter(NewEligibleConverter(ec => 
                        ec.WithPreviousProviderIdentifier(sourceProviderId)
                            .WithProviderId(destinationProviderId)))));
            
            datasetTable.Rows
                .Count
                .Should()
                .Be(2);
            
            datasetTable.Rows
                .Last()
                .Should()
                .BeEquivalentTo(NewRowLoadResult(row => row
                    .WithIdentifierFieldType(IdentifierFieldType.UKPRN)
                    .WithIdentifier(destinationProviderId)
                    .WithFields(NewField(fieldName, destinationProviderId), 
                    fieldOne, 
                    fieldTwo)));
        }

        [TestMethod]
        public async Task SaveContentsCreatesAndIndexesANewDatasetVersionAndUploadsANewXlsBlob()
        {
            Reference author = NewReference();
            Dataset dataset = NewDataset(_ => _.WithHistory(NewDatasetVersion(ver => ver.WithVersion(97)),
                NewDatasetVersion(ver => ver.WithVersion(98))));
            DatasetVersion currentVersion = dataset.Current;
            currentVersion.Version = 99;
            DatasetDefinition datasetDefinition = NewDatasetDefinition();
            
            TableLoadResult datasetTable = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(),
                NewRowLoadResult(),
                NewRowLoadResult(),
                NewRowLoadResult()));

            byte[] expectedExcelData = NewRandomString().AsUTF8Bytes();
            
            GivenTheDatasetData(datasetTable);
            AndTheDatasetSavesSuccessfully(dataset);
            AndTheExcelDataForTheDatasetData(datasetDefinition, datasetTable, expectedExcelData);

            Mock<ICloudBlob> blob = NewBlob();

            string currentBlobName = dataset.Current.BlobName;
            
            AndTheBlobReference($"{dataset.Id}/v100/{currentBlobName}", blob.Object);

            await WhenTheContentsAreSaved(author, datasetDefinition, dataset);

            dataset
                .History
                .Count
                .Should()
                .Be(4);

            DatasetVersion newVersion = dataset.Current;
            DatasetVersion expectedNewVersion = (DatasetVersion) currentVersion.Clone();
            expectedNewVersion.Author = author;
            expectedNewVersion.RowCount = datasetTable.Rows.Count;
            expectedNewVersion.Version++;
            expectedNewVersion.ChangeType = DatasetChangeType.ConverterWizard;
            expectedNewVersion.BlobName = $"{dataset.Id}/v100/{currentBlobName}";
            
            newVersion
                .Should()
                .BeEquivalentTo(expectedNewVersion);

            dataset
                .History
                .Last()
                .Should()
                .BeSameAs(dataset.Current);

            _indexer.Verify(_ => _.IndexDatasetAndVersion(dataset), 
                Times.Once);

            _lastUploadedBlobData
                .Should()
                .BeEquivalentTo(expectedExcelData);
            
            AndTheBlobHasMetadataMatchingTheDataset(blob.Object, dataset, datasetDefinition);
            
            blob.Verify(_ => _.SetMetadata(null, null, null), Times.Once);
        }

        private void AndTheBlobHasMetadataMatchingTheDataset(ICloudBlob blob,
            Dataset dataset,
            DatasetDefinition datasetDefinition)
        {
            IDictionary<string, string> metadata = blob.Metadata;
            
            AssertThatDictionaryContainsEntry(metadata, ("dataDefinitionId", datasetDefinition.Id));
            AssertThatDictionaryContainsEntry(metadata, ("datasetId", dataset.Id));
            AssertThatDictionaryContainsEntry(metadata, ("authorId", dataset.Current.Author.Id));
            AssertThatDictionaryContainsEntry(metadata, ("authorName", dataset.Current.Author.Name));
            AssertThatDictionaryContainsEntry(metadata, ("name", dataset.Current.BlobName));
            AssertThatDictionaryContainsEntry(metadata, ("description", dataset.Description));
            AssertThatDictionaryContainsEntry(metadata, ("fundingStreamId", datasetDefinition.FundingStreamId));
            AssertThatDictionaryContainsEntry(metadata, ("converterWizard", true.ToString()));
        }

        private void AssertThatDictionaryContainsEntry(IDictionary<string, string> dictionary,
            (string key, string value) expectedValue)
        {
            dictionary
                .ContainsKey(expectedValue.key)
                .Should()
                .BeTrue();

            dictionary[expectedValue.key]
                .Should()
                .BeEquivalentTo(expectedValue.value);
        }
        

        private async Task WhenTheContentsAreSaved(Reference author,
            DatasetDefinition datasetDefinition,
            Dataset dataset)
            => await _builder.SaveContents(author, datasetDefinition, dataset);

        private RowCopyResult WhenTheRowIsCopied(string fieldName,
            string sourceProviderId,
            string destinationProviderId) =>
            _builder.CopyRow(fieldName, sourceProviderId, destinationProviderId);

        private void AndTheBlobReference(string path,
            ICloudBlob blob)
            => _blobs.Setup(_ => _.GetBlockBlobReference(path))
                .Returns(blob);

        private void AndTheDatasetSavesSuccessfully(Dataset dataset)
            => _datasets.Setup(_ => _.SaveDataset(dataset))
                .ReturnsAsync(HttpStatusCode.OK);

        private void AndTheExcelDataForTheDatasetData(DatasetDefinition datasetDefinition,
            TableLoadResult datasetData,
            byte[] excelData)
            => _writer.Setup(_ => _.Write(datasetDefinition, 
                    It.Is<IEnumerable<TableLoadResult>>(tables =>
                    tables.SequenceEqual(new [] { datasetData }))))
                .Returns(excelData);

        private IEnumerable<string> WhenTheExistingIdentifierValuesAreQueried(string identifierFieldName)
        {
            return _builder.GetExistingIdentifierValues(identifierFieldName);
        }

        private void GivenTheDatasetData(params TableLoadResult[] tables)
        {
            _builder.DatasetData = tables;
        }

        private async Task WhenTheOriginalDatasetIsLoaded(Dataset dataset,
            DatasetDefinition datasetDefinition)
        {
            await _builder.LoadOriginalDataset(dataset, datasetDefinition);
        }

        private void GivenTheBlob(string path,
            ICloudBlob cloudBlob)
        {
            _blobs.Setup(_ => _.GetBlobReferenceFromServerAsync(path))
                .ReturnsAsync(cloudBlob);
        }

        private void AndTheBlob(string path,
            ICloudBlob blob)
            => _blobs.Setup(_ => _.GetBlockBlobReference(path))
                .Returns(blob);

        private void AndTheBlobStream(ICloudBlob cloudBlob,
            Stream stream)
        {
            _blobs.Setup(_ => _.DownloadToStreamAsync(cloudBlob))
                .ReturnsAsync(stream);
        }

        private void AndTheTableLoadResults(Stream excelStream,
            DatasetDefinition datasetDefinition,
            IEnumerable<TableLoadResult> results)
        {
            _reader.Setup(_ => _.Read(excelStream, datasetDefinition))
                .Returns(results);
        }

        private Mock<ICloudBlob> NewBlob()
        {
            Mock<ICloudBlob> blob = new Mock<ICloudBlob>();

            Dictionary<string, string> metaData = new Dictionary<string, string>();

            blob.Setup(_ => _.Metadata)
                .Returns(metaData);
            blob.Setup(_ => _.UploadFromStreamAsync(It.IsAny<Stream>()))
                .Callback<Stream>(stream =>
                {
                    _lastUploadedBlobData = stream.ReadAllBytes();
                });
            
            return blob;
        }

        private Reference NewReference() => new ReferenceBuilder().Build();

        private RowCopyResult NewRowCopyResult(Action<RowCopyResultBuilder> setUp = null)
        {
            RowCopyResultBuilder rowCopyResultBuilder = new RowCopyResultBuilder();

            setUp?.Invoke(rowCopyResultBuilder);

            return rowCopyResultBuilder.Build();
        }

        private RowLoadResult NewRowLoadResult(Action<RowLoadResultBuilder> setUp = null)
        {
            RowLoadResultBuilder rowLoadResultBuilder = new RowLoadResultBuilder();

            setUp?.Invoke(rowLoadResultBuilder);

            return rowLoadResultBuilder.Build();
        }

        private (string fieldName, object value) NewField(string fieldName = null,
            object value = null)
        {
            return (fieldName ?? NewRandomString(), value ?? NewRandomString());
        }

        private DatasetDefinition NewDatasetDefinition(Action<DatasetDefinitionBuilder> setUp = null)
        {
            DatasetDefinitionBuilder datasetDefinitionBuilder = new DatasetDefinitionBuilder();

            setUp?.Invoke(datasetDefinitionBuilder);

            return datasetDefinitionBuilder.Build();
        }

        private Dataset NewDataset(Action<DatasetBuilder> setUp = null)
        {
            DatasetVersion current = NewDatasetVersion();

            DatasetBuilder datasetBuilder = new DatasetBuilder()
                .WithCurrent(current)
                .WithHistory(current);

            setUp?.Invoke(datasetBuilder);

            return datasetBuilder.Build();
        }

        private DatasetVersion NewDatasetVersion(Action<DatasetVersionBuilder> setUp = null)
        {
            DatasetVersionBuilder datasetVersionBuilder = new DatasetVersionBuilder();

            setUp?.Invoke(datasetVersionBuilder);

            return datasetVersionBuilder.Build();
        }

        private TableLoadResult NewTableLoadResult(Action<TableLoadResultBuilder> setUp = null)
        {
            TableLoadResultBuilder tableLoadResultBuilder = new TableLoadResultBuilder();

            setUp?.Invoke(tableLoadResultBuilder);

            return tableLoadResultBuilder.Build();
        }

        private EligibleConverter NewEligibleConverter(Action<EligibleConverterBuilder> setUp = null)
        {
            EligibleConverterBuilder eligibleConverterBuilder = new EligibleConverterBuilder();

            setUp?.Invoke(eligibleConverterBuilder);

            return eligibleConverterBuilder.Build();
        }

        private string NewRandomString()
        {
            return new RandomString();
        }
    }
}