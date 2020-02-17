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
                CalculationRelationships = _relationships ?? new CalculationRelationship[0]
            };
        }
    }
}