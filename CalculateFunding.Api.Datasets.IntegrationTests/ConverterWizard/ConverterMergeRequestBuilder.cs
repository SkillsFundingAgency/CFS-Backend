using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    public class ConverterMergeRequestBuilder : TestEntityBuilder
    {
        private string _providerVersionId;
        private Reference _author;
        private string _datasetId;
        private string _datasetRelationshipId;
        private string _version;

        public ConverterMergeRequestBuilder WithProviderVersionId(string providerVersionId)
        {
            _providerVersionId = providerVersionId;

            return this;
        }

        public ConverterMergeRequestBuilder WithDatasetId(string datasetId)
        {
            _datasetId = datasetId;

            return this;
        }

        public ConverterMergeRequestBuilder WithVersion(string version)
        {
            _version = version;

            return this;
        }

        public ConverterMergeRequestBuilder WithDatasetRelationshipId(string datasetRelationshipId)
        {
            _datasetRelationshipId = datasetRelationshipId;

            return this;
        }

        public ConverterMergeRequestBuilder WithAuthor(Reference author)
        {
            _author = author;

            return this;
        }
        
        
        public ConverterMergeRequest Build()
        {
            return new ConverterMergeRequest
            {
                ProviderVersionId = _providerVersionId,
                Author = _author,
                DatasetId = _datasetId,
                DatasetRelationshipId = _datasetRelationshipId,
                Version = _version
            };
        }
    }
}