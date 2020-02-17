using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Compiler.UnitTests
{
    public class CalculationRelationshipBuilder : TestEntityBuilder
    {
        private string _calculationOneId;
        private string _calculationTwoId;

        public CalculationRelationshipBuilder WithCalculationOneId(string calculationId)
        {
            _calculationOneId = calculationId;

            return this;
        }
        
        public CalculationRelationshipBuilder WithCalculationTwoId(string calculationId)
        {
            _calculationTwoId = calculationId;

            return this;
        }
        
        public CalculationRelationship Build()
        {
            return new CalculationRelationship
            {
                CalculationOneId = _calculationOneId ?? NewRandomString(),
                CalculationTwoId = _calculationTwoId ?? NewRandomString()
            };
        }
    }
}