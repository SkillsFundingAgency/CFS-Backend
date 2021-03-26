using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class ConverterMergeRequestBuilder : TestEntityBuilder
    {
        private Reference _author;
        private string _datasetId;
        private string _datasetRelationshipId;
        private string _providerVersionId;
        private string _version;
        private bool _withoutVersion;
        private bool _withoutProviderVersionId;
        private bool _withoutDatasetId;
        private bool _withoutDatasetRelationshipId;

        public ConverterMergeRequestBuilder WithoutDatasetRelationshipId()
        {
            _withoutDatasetRelationshipId = true;

            return this;
        }

        public ConverterMergeRequestBuilder WithoutDatasetId()
        {
            _withoutDatasetId = true;

            return this;
        }

        public ConverterMergeRequestBuilder WithoutProviderVersionId()
        {
            _withoutProviderVersionId = true;

            return this;
        }

        public ConverterMergeRequestBuilder WithoutVersion()
        {
            _withoutVersion = true;

            return this;
        }

        public ConverterMergeRequestBuilder WithAuthor(Reference author)
        {
            _author = author;

            return this;
        }

        public ConverterMergeRequestBuilder WithDatasetId(string datasetId)
        {
            _datasetId = datasetId;

            return this;
        }

        public ConverterMergeRequestBuilder WithDatasetRelationshipId(string datasetRelationshipId)
        {
            _datasetRelationshipId = datasetRelationshipId;

            return this;
        }

        public ConverterMergeRequestBuilder WithProviderVersionId(string providerVersionId)
        {
            _providerVersionId = providerVersionId;

            return this;
        }

        public ConverterMergeRequestBuilder WithVersion(string version)
        {
            _version = version;

            return this;
        }

        public ConverterMergeRequest Build() =>
            new ConverterMergeRequest
            {
                Author = _author,
                DatasetId = _withoutDatasetId ? null : _datasetId ?? NewRandomString(),
                DatasetRelationshipId = _withoutDatasetRelationshipId ? null : _datasetRelationshipId ?? NewRandomString(),
                ProviderVersionId = _withoutProviderVersionId ? null : _providerVersionId ?? NewRandomString(),
                Version = _withoutVersion ? null : _version ?? NewRandomString()
            };
    }
}