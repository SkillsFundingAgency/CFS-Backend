using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.FundingDataZone.UnitTests
{
    [TestClass]
    public class DatasetsForFundingStreamServiceTests
    {
        private Mock<IPublishingAreaRepository> _publishingArea;

        private DatasetsForFundingStreamService _service;

        [TestInitialize]
        public void SetUp()
        {
            _publishingArea = new Mock<IPublishingAreaRepository>();

            _service = new DatasetsForFundingStreamService(_publishingArea.Object);
        }

        [TestMethod]
        public void GuardsAgainstNoFundingStreamIdBeingSupplied()
        {
            Func<Task<IEnumerable<Dataset>>> invocation = () => WhenTheDatasetsForFundingStreamAreQueried(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("fundingStreamId");
        }

        [TestMethod]
        public async Task QueriesDatasetMetadataGroupedByNameAndMapsIntoDatasetResponses()
        {
            string fundingStreamId = new RandomString();
            
            Dataset datasetOne = NewDataset();
            Dataset datasetTwo = NewDataset();
            Dataset datasetThree = NewDataset();

            PublishingAreaDatasetMetadata[] metadata = NewMetadataForDatasets(datasetOne, datasetTwo, datasetThree);
            
            GivenTheMetaDataForTheFundingStream(fundingStreamId, metadata);

            IEnumerable<Dataset> actualDatasets = await WhenTheDatasetsForFundingStreamAreQueried(fundingStreamId);

            actualDatasets
                .Should()
                .BeEquivalentTo(new[]
                {
                    datasetOne, datasetTwo, datasetThree
                },
                    opt => 
                        opt.Using<DateTime>(_ => _.Subject
                            .Should()
                            .BeCloseTo(_.Expectation, 1000))
                        .WhenTypeIs<DateTime>());
        }

        private void GivenTheMetaDataForTheFundingStream(string fundingStreamId,
            PublishingAreaDatasetMetadata[] metadata)
        {
            _publishingArea.Setup(_ => _.GetDatasetMetadata(fundingStreamId))
                .ReturnsAsync(metadata);
        }

        private async Task<IEnumerable<Dataset>> WhenTheDatasetsForFundingStreamAreQueried(string fundingStreamId)
            => await _service.GetDatasetsForFundingStream(fundingStreamId);
        
        private Dataset NewDataset(Action<DatasetBuilder> setUp = null)
        {
            DatasetBuilder datasetBuilder = new DatasetBuilder();

            setUp?.Invoke(datasetBuilder);
            
            return datasetBuilder.Build();
        }

        private PublishingAreaDatasetMetadata[] NewMetadataForDatasets(params Dataset[] datasets)
        {
            return datasets.SelectMany(NewDatasetVersionsForDataset).ToArray();
        }

        private IEnumerable<PublishingAreaDatasetMetadata> NewDatasetVersionsForDataset(Dataset dataset)
        {
            return typeof(Dataset).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(_ => _.Name != "Properties")
                .Select(_ => NewMetadata(meta => meta.WithDataSetName(dataset.TableName)
                    .WithExtendedProperty(MapToMetadataName(_.Name))
                    .WithExtendedPropertyValue(_.GetValue(dataset)?.ToString())))
                .Concat(dataset.Properties.Select(_ => NewMetadata(meta => 
                    meta.WithDataSetName(dataset.TableName)
                        .WithExtendedProperty($"Prop_{_.Key}")
                        .WithExtendedPropertyValue(_.Value))));
        }

        private string MapToMetadataName(string datasetPropertyName)
            => datasetPropertyName switch
            {
                "IdentifierColumnName" => "ProviderIdentifierColumnName",
                "IdentifierType" => "ProviderIdentifierType",
                "Version" => "SnapshotVersion",
                _ => datasetPropertyName
            };

        private PublishingAreaDatasetMetadata NewMetadata(Action<PublishingAreaDatasetMetadataBuilder> setUp = null)
        {
            PublishingAreaDatasetMetadataBuilder publishingAreaDatasetMetadataBuilder = new PublishingAreaDatasetMetadataBuilder();

            setUp?.Invoke(publishingAreaDatasetMetadataBuilder);
            
            return publishingAreaDatasetMetadataBuilder.Build();
        }
    }
}