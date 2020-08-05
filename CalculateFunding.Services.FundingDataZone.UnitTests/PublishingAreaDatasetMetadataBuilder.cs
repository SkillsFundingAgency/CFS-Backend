using CalculateFunding.Services.FundingDataZone.SqlModels;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.FundingDataZone.UnitTests
{
    public class PublishingAreaDatasetMetadataBuilder : TestEntityBuilder
    {
        private string _dataSetName;
        private string _extendedProperty;
        private string _extendedPropertyValue;

        public PublishingAreaDatasetMetadataBuilder WithDataSetName(string dataSetName)
        {
            _dataSetName = dataSetName;

            return this;
        }

        public PublishingAreaDatasetMetadataBuilder WithExtendedProperty(string extendedProperty)
        {
            _extendedProperty = extendedProperty;

            return this;
        }
        
        public PublishingAreaDatasetMetadataBuilder WithExtendedPropertyValue(string extendedPropertyValue)
        {
            _extendedPropertyValue = extendedPropertyValue;

            return this;
        }
        
        public PublishingAreaDatasetMetadata Build()
        {
            return new PublishingAreaDatasetMetadata
            {
                DatasetName = _dataSetName ?? NewRandomString(),
                ExtendedProperty = _extendedProperty ?? NewRandomString(),
                ExtendedPropertyValue = _extendedPropertyValue ?? NewRandomString()
            };
        }
    }
}