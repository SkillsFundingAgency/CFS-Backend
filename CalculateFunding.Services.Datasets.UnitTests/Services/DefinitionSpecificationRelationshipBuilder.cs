using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class DefinitionSpecificationRelationshipBuilder : TestEntityBuilder
    {
        private Reference _datasetDefinition;
        private DatasetRelationshipVersion _datasetVersion;

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
        
        public DefinitionSpecificationRelationship Build()
        {
            return new DefinitionSpecificationRelationship
            {
                DatasetDefinition = _datasetDefinition,
                DatasetVersion = _datasetVersion
            };
        }
    }
}