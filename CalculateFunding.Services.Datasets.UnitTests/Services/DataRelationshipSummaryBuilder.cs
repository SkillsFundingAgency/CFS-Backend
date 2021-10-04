using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;
using CalculateFunding.Services.Datasets.Services.UnitTests;

namespace CalculateFunding.Services.Datasets.Services
{
    public class DataRelationshipSummaryBuilder : TestEntityBuilder
    {
        private string _id;
        private Reference _relationship;
        private DatasetDefinition _datasetDefinition;
        private bool _definesScope;
        private DatasetRelationshipType? _datasetRelationshipType = null;
        private PublishedSpecificationConfiguration _publishedSpecificationConfiguration = null;

        public DataRelationshipSummaryBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

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

        public DataRelationshipSummaryBuilder WithRelationshipType(DatasetRelationshipType relationshipType)
        {
            _datasetRelationshipType = relationshipType;

            return this;
        }

        public DataRelationshipSummaryBuilder WithPublishedSpecificationConfiguration(PublishedSpecificationConfiguration publishedSpecificationConfiguration)
        {
            _publishedSpecificationConfiguration = publishedSpecificationConfiguration;

            return this;
        }

        public DatasetRelationshipSummary Build()
        {
            return new DatasetRelationshipSummary
            {
                Id = _id ?? new RandomString(),
                DatasetDefinition = _datasetDefinition,
                Relationship = _relationship ?? new ReferenceBuilder()
                                   .Build(),
                DefinesScope = _definesScope,
                RelationshipType = _datasetRelationshipType ?? DatasetRelationshipType.Uploaded,
                PublishedSpecificationConfiguration = _publishedSpecificationConfiguration
            };
        }
    }
}