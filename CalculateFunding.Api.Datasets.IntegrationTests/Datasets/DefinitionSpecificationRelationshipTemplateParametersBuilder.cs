using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Datasets
{
    public class DefinitionSpecificationRelationshipTemplateParametersBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;
        private string _definitionId;
        private string _definitionName;
        private string _datasetId;
        private string _datasetName;
        private string _specificationId;
        private string _specificationName;
        private string _description;
        private bool? _converterEnabled;
        private IEnumerable<uint> _calculationIds;
        private IEnumerable<uint> _fundingLineIds;
        private string _targetSpecificationId;
        private DatasetRelationshipType _relationshipType;
        private PublishedSpecificationConfiguration _publishedSpecificationConfiguration;

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithDefinitionId(string definitionId)
        {
            _definitionId = definitionId;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithDefinitionName(string definitionName)
        {
            _definitionName = definitionName;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithDatasetId(string datasetId)
        {
            _datasetId = datasetId;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithDatasetName(string datasetName)
        {
            _datasetName = datasetName;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithSpecificationName(string specificationName)
        {
            _specificationName = specificationName;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithTargetSpecificationId(string targetSpecificationId)
        {
            _targetSpecificationId = targetSpecificationId;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithCalculationIds(IEnumerable<uint> calulationIds)
        {
            _calculationIds = calulationIds;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithFundingLineIds(IEnumerable<uint> fundingLineIds)
        {
            _fundingLineIds = fundingLineIds;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithConverterEnabled(bool converterEnabled)
        {
            _converterEnabled = converterEnabled;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithPublishedSpecificationConfiguration(PublishedSpecificationConfiguration publishedSpecificationConfiguration)
        {
            _publishedSpecificationConfiguration = publishedSpecificationConfiguration;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithRelationshipType(DatasetRelationshipType relationshipType)
        {
            _relationshipType = relationshipType;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParameters Build()
            => new DefinitionSpecificationRelationshipTemplateParameters
            {
                Id = _id ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                Description = _description ?? NewRandomString(),
                DefinitionId = _definitionId ?? NewRandomString(),
                DefinitionName = _definitionName ?? NewRandomString(),
                DatasetId = _datasetId ?? NewRandomString(),
                DatasetName = _datasetName ?? NewRandomString(),
                SpecificationId = _specificationId ?? NewRandomString(),
                SpecificationName = _specificationName ?? NewRandomString(),
                ConverterEnabled = _converterEnabled.GetValueOrDefault(NewRandomFlag()),
                PublishedSpecificationConfiguration = new PublishedSpecificationConfiguration
                {
                    SpecificationId = _targetSpecificationId ?? NewRandomString(),
                    Calculations = _calculationIds.IsNullOrEmpty() ? _calculationIds.Select(_ => new PublishedSpecificationItem { TemplateId = _ }) : ArraySegment<PublishedSpecificationItem>.Empty,
                    FundingLines = _fundingLineIds.IsNullOrEmpty() ? _fundingLineIds.Select(_ => new PublishedSpecificationItem { TemplateId = _ }) : ArraySegment<PublishedSpecificationItem>.Empty,
                },
                DatasetRelationshipType = _relationshipType
            };
    }
}