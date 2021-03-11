using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Compiler.UnitTests
{
    public class CalculationEnumRelationshipBuilder : TestEntityBuilder
    {
        private Calculation _calculation;
        private Enum _enum;

        public CalculationEnumRelationshipBuilder WithCalculation(Calculation calculation)
        {
            _calculation = calculation;

            return this;
        }
        
        public CalculationEnumRelationshipBuilder WithEnum(Enum enumNameValue)
        {
            _enum = enumNameValue;

            return this;
        }
        
        public CalculationEnumRelationship Build()
        {
            return new CalculationEnumRelationship
            {
                Calculation = _calculation,
                Enum = _enum
            };
        }
    }
}