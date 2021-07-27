using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Datasets
{
    public class CreateDefinitionSpecificationRelationshipModelBuilder : TestEntityBuilder
    {
        private string _datasetDefinitionId;

        private IEnumerable<uint> _calculationIds;

        private IEnumerable<uint> _fundingLineIds;

        private DatasetRelationshipType _relationshipType = DatasetRelationshipType.Uploaded;

        private string _specificationId;

        private string _targetSpecificationId;

        public CreateDefinitionSpecificationRelationshipModelBuilder WithDatasetDefinitionId(string datasetDefinitionId)
        {
            _datasetDefinitionId = datasetDefinitionId;

            return this;
        }

        public CreateDefinitionSpecificationRelationshipModelBuilder WithRelationshipType(DatasetRelationshipType relationshipType)
        {
            _relationshipType = relationshipType;

            return this;
        }

        public CreateDefinitionSpecificationRelationshipModelBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public CreateDefinitionSpecificationRelationshipModelBuilder WithTargetSpecificationId(string targetSpecificationId)
        {
            _targetSpecificationId = targetSpecificationId;

            return this;
        }

        public CreateDefinitionSpecificationRelationshipModelBuilder WithCalculationIds(IEnumerable<uint> calulationIds)
        {
            _calculationIds = calulationIds;

            return this;
        }

        public CreateDefinitionSpecificationRelationshipModelBuilder WithFundingLineIds(IEnumerable<uint> fundingLineIds)
        {
            _fundingLineIds = fundingLineIds;

            return this;
        }

        public CreateDefinitionSpecificationRelationshipModel Build() =>
            new CreateDefinitionSpecificationRelationshipModel
            {
                DatasetDefinitionId = _datasetDefinitionId ?? NewRandomString(),
                Description = NewRandomString(),
                Name = NewRandomString(),
                RelationshipType = _relationshipType,
                SpecificationId = _specificationId ?? NewRandomString(),
                TargetSpecificationId = _targetSpecificationId ?? NewRandomString(),
                CalculationIds = _calculationIds ?? ArraySegment<uint>.Empty,
                FundingLineIds = _fundingLineIds ?? ArraySegment<uint>.Empty
            };
    }
}
