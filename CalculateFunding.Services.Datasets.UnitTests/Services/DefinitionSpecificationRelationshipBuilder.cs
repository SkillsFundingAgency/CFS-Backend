using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class DefinitionSpecificationRelationshipBuilder : TestEntityBuilder
    {
        private Reference _datasetDefinition;
        private DatasetRelationshipVersion _datasetVersion;
        private bool _isSetAsProviderData;
        private string _id;

        public DefinitionSpecificationRelationshipBuilder WithDatasetDefinition(Reference datasetDefinition)
        {
            _datasetDefinition = datasetDefinition;

            return this;
        }

        public DefinitionSpecificationRelationshipBuilder WithDatasetVersion(DatasetRelationshipVersion datasetVersion)
        {
            _datasetVersion = datasetVersion;

            return this;
        }

        public DefinitionSpecificationRelationshipBuilder WithIsSetAsProviderData(bool isSetAsProviderData)
        {
            _isSetAsProviderData = isSetAsProviderData;

            return this;
        }

        public DefinitionSpecificationRelationshipBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        public DefinitionSpecificationRelationship Build()
        {
            return new DefinitionSpecificationRelationship
            {
                DatasetDefinition = _datasetDefinition,
                DatasetVersion = _datasetVersion,
                IsSetAsProviderData = _isSetAsProviderData,
                Id = _id
            };
        }
    }
}