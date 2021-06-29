using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;
using System;

namespace CalculateFunding.Services.Datasets.Services
{
    public class DefinitionSpecificationRelationshipVersionBuilder : TestEntityBuilder
    {
        private Reference _specification;
        private Reference _datasetDefinition;
        private DatasetRelationshipVersion _datasetVersion;
        private bool _isSetAsProviderData;
        private string _relationshipId;
        private bool? _converterEnabled;
        private PublishedSpecificationConfiguration _publishedSpecificationConfiguration;
        private string _name;
        private DatasetRelationshipType? _relationshipType;
        private string _description;
        private Reference _author;
        private DateTimeOffset? _lastUpdated;

        public DefinitionSpecificationRelationshipVersionBuilder WithConverterEnabled(bool converterEnabled)
        {
            _converterEnabled = converterEnabled;

            return this;
        }

        public DefinitionSpecificationRelationshipVersionBuilder WithSpecification(Reference specification)
        {
            _specification = specification;

            return this;
        }

        public DefinitionSpecificationRelationshipVersionBuilder WithDatasetDefinition(Reference datasetDefinition)
        {
            _datasetDefinition = datasetDefinition;

            return this;
        }

        public DefinitionSpecificationRelationshipVersionBuilder WithDatasetVersion(DatasetRelationshipVersion datasetVersion)
        {
            _datasetVersion = datasetVersion;

            return this;
        }

        public DefinitionSpecificationRelationshipVersionBuilder WithIsSetAsProviderData(bool isSetAsProviderData)
        {
            _isSetAsProviderData = isSetAsProviderData;

            return this;
        }

        public DefinitionSpecificationRelationshipVersionBuilder WithRelationshipId(string relationshipId)
        {
            _relationshipId = relationshipId;

            return this;
        }

        public DefinitionSpecificationRelationshipVersionBuilder WithPublishedSpecificationConfiguration(PublishedSpecificationConfiguration publishedSpecificationConfiguration)
        {
            _publishedSpecificationConfiguration = publishedSpecificationConfiguration;

            return this;
        }

        public DefinitionSpecificationRelationshipVersionBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public DefinitionSpecificationRelationshipVersionBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public DefinitionSpecificationRelationshipVersionBuilder WithRelationshipType(DatasetRelationshipType? relationshipType)
        {
            _relationshipType = relationshipType;

            return this;
        }

        public DefinitionSpecificationRelationshipVersionBuilder WithAuthor(Reference author)
        {
            _author = author;

            return this;
        }

        public DefinitionSpecificationRelationshipVersionBuilder WithLastUpdated(DateTimeOffset? lastUpdated)
        {
            _lastUpdated = lastUpdated;

            return this;
        }

        public DefinitionSpecificationRelationshipVersion Build()
        {
            return new DefinitionSpecificationRelationshipVersion
            {
                ConverterEnabled = _converterEnabled.GetValueOrDefault(NewRandomFlag()),
                DatasetDefinition = _datasetDefinition,
                DatasetVersion = _datasetVersion,
                IsSetAsProviderData = _isSetAsProviderData,
                Specification = _specification,
                RelationshipId = _relationshipId ?? NewRandomString(),
                PublishedSpecificationConfiguration = _publishedSpecificationConfiguration,
                Name = _name ?? NewRandomString(),
                Description = _description ?? NewRandomString(),
                Author = _author ?? new Reference(NewRandomString(), NewRandomString()),
                LastUpdated = _lastUpdated ?? NewRandomDateTime(),
                RelationshipType = _relationshipType ?? NewRandomEnum<DatasetRelationshipType>(DatasetRelationshipType.ReleasedData)
            };
        }
    }
}