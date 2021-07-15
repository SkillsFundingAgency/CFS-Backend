using System.Collections.Generic;
using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;
using System;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    public class SpecificationCalculationRelationshipBuilder : TestEntityBuilder
    {
        private IEnumerable<CalculationRelationship> _relationships;
        private IEnumerable<Calculation> _calculations;
        private IEnumerable<FundingLineCalculationRelationship> _fundingLineCalculationRelationships;
        private IEnumerable<FundingLine> _fundingLines;
        private Specification _specification;
        private IEnumerable<CalculationDataFieldRelationship> _calculationDatafieldRelationships;
        private IEnumerable<DatasetDataFieldRelationship> _datasetDataFieldRelationships;
        private IEnumerable<DatasetDatasetDefinitionRelationship> _datasetDatasetDefinitionRelationships;
        private IEnumerable<CalculationEnumRelationship> _calculationEnumRelationships;
        private IEnumerable<DatasetRelationship> _datasetRelationships;
        private IEnumerable<DatasetRelationshipDataFieldRelationship> _datasetRelationshipDataFieldRelationships;

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

        public SpecificationCalculationRelationshipBuilder WithFundingLines(params FundingLine[] fundingLines)
        {
            _fundingLines = fundingLines;

            return this;
        }

        public SpecificationCalculationRelationshipBuilder WithFundingLineCalculationRelationships(params FundingLineCalculationRelationship[] fundingLineCalculationRelationships)
        {
            _fundingLineCalculationRelationships = fundingLineCalculationRelationships;

            return this;
        }

        public SpecificationCalculationRelationshipBuilder WithCalculationDataFieldRelationships(params CalculationDataFieldRelationship[] calculationDatafieldRelationships)
        {
            _calculationDatafieldRelationships = calculationDatafieldRelationships;

            return this;
        }

        public SpecificationCalculationRelationshipBuilder WithCalculationEnumRelationships(params CalculationEnumRelationship[] calculationEnumRelationships)
        {
            _calculationEnumRelationships = calculationEnumRelationships;

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

        public SpecificationCalculationRelationshipBuilder WithDatasetRelationships(params DatasetRelationship[] datasetRelationships)
        {
            _datasetRelationships = datasetRelationships;

            return this;
        }

        public SpecificationCalculationRelationshipBuilder WithDatasetRelationshipDataFieldRelationships(
            params DatasetRelationshipDataFieldRelationship[] datasetRelationshipDataFieldRelationships)
        {
            _datasetRelationshipDataFieldRelationships = datasetRelationshipDataFieldRelationships;

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
                Calculations = _calculations ?? Array.Empty<Calculation>(),
                FundingLines = _fundingLines ?? Array.Empty<FundingLine>(),
                FundingLineRelationships = _fundingLineCalculationRelationships ?? Array.Empty<FundingLineCalculationRelationship>(),
                CalculationRelationships = _relationships ?? Array.Empty<CalculationRelationship>(),
                CalculationDataFieldRelationships = _calculationDatafieldRelationships ?? Array.Empty<CalculationDataFieldRelationship>(),
                DatasetDataFieldRelationships = _datasetDataFieldRelationships ?? Array.Empty<DatasetDataFieldRelationship>(),
                CalculationEnumRelationships = _calculationEnumRelationships ?? Array.Empty<CalculationEnumRelationship>(),
                DatasetDatasetDefinitionRelationships = _datasetDatasetDefinitionRelationships ?? Array.Empty<DatasetDatasetDefinitionRelationship>(),
                DatasetRelationships = _datasetRelationships ?? Array.Empty<DatasetRelationship>(),
                DatasetRelationshipDataFieldRelationships = _datasetRelationshipDataFieldRelationships ?? Array.Empty<DatasetRelationshipDataFieldRelationship>()
            };
        }
    }
}