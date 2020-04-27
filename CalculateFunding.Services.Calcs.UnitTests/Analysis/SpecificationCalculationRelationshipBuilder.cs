using System.Collections.Generic;
using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    public class SpecificationCalculationRelationshipBuilder : TestEntityBuilder
    {
        private IEnumerable<CalculationRelationship> _relationships;
        private IEnumerable<Calculation> _calculations;
        private Specification _specification;
        private IEnumerable<CalculationDataFieldRelationship> _calculationDatafieldRelationships;
        private IEnumerable<DatasetDataFieldRelationship> _datasetDataFieldRelationships;
        private IEnumerable<DatasetDatasetDefinitionRelationship> _datasetDatasetDefinitionRelationships;

        public SpecificationCalculationRelationshipBuilder WithCalculations(params Calculation[] calculations)
        {
            _calculations = calculations;

            return this;
        }

        public SpecificationCalculationRelationshipBuilder WithCalculationRelationships(params CalculationRelationship[] relationships)
        {
            _relationships = relationships;

            return this;
        }

        public SpecificationCalculationRelationshipBuilder WithCalculationDataFieldRelationships(params CalculationDataFieldRelationship[] calculationDatafieldRelationships)
        {
            _calculationDatafieldRelationships = calculationDatafieldRelationships;

            return this;
        }

        public SpecificationCalculationRelationshipBuilder WithDatasetDataFieldRelationships(params DatasetDataFieldRelationship[] datasetDataFieldRelationships)
        {
            _datasetDataFieldRelationships = datasetDataFieldRelationships;

            return this;
        }

        public SpecificationCalculationRelationshipBuilder WithDatasetDatasetDefinitionRelationships(params DatasetDatasetDefinitionRelationship[] datasetDatasetDefinitionRelationships)
        {
            _datasetDatasetDefinitionRelationships = datasetDatasetDefinitionRelationships;

            return this;
        }

        public SpecificationCalculationRelationshipBuilder WithSpecification(Specification specification)
        {
            _specification = specification;

            return this;
        }

        public SpecificationCalculationRelationships Build()
        {
            return new SpecificationCalculationRelationships
            {
                Specification = _specification,
                Calculations = _calculations ?? new Calculation[0],
                CalculationRelationships = _relationships ?? new CalculationRelationship[0],
                CalculationDataFieldRelationships = _calculationDatafieldRelationships ?? new CalculationDataFieldRelationship[0],
                DatasetDataFieldRelationships = _datasetDataFieldRelationships ?? new DatasetDataFieldRelationship[0],
                DatasetDatasetDefinitionRelationships = _datasetDatasetDefinitionRelationships ?? new DatasetDatasetDefinitionRelationship[0]
            };
        }
    }
}