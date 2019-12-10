using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class DataRelationshipSummaryBuilder : TestEntityBuilder
    {
        private Reference _relationship;
        private DatasetDefinition _datasetDefinition;
        private bool _definesScope;
        
        public DataRelationshipSummaryBuilder WithDefinesScope(bool definesScope)
        {
            _definesScope = definesScope;

            return this;
        }

        public DataRelationshipSummaryBuilder WithDatasetDefinition(DatasetDefinition datasetDefinition)
        {
            _datasetDefinition = datasetDefinition;

            return this;
        }

        public DataRelationshipSummaryBuilder WithRelationship(Reference relationship)
        {
            _relationship = relationship;

            return this;
        }
        
        public DatasetRelationshipSummary Build()
        {
            return new DatasetRelationshipSummary
            {
                DatasetDefinition = _datasetDefinition,
                Relationship = _relationship ?? new ReferenceBuilder()
                                   .Build(),
                DefinesScope = _definesScope
            };
        }
    }
}