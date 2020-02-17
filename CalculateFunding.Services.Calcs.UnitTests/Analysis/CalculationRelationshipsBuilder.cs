using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    public class CalculationRelationshipsBuilder : TestEntityBuilder
    {
        private string _calculationOneId;
        private string _calculationTwoId;

        public CalculationRelationshipsBuilder WithCalculationOneId(string id)
        {
            _calculationOneId = id;

            return this;
        }

        public CalculationRelationshipsBuilder WithCalculationTwoId(string id)
        {
            _calculationTwoId = id;

            return this;
        }

        public CalculationRelationship Build()
        {
            return new CalculationRelationship
            {
                CalculationOneId = _calculationOneId ?? NewRandomString(),
                CalculationTwoId = _calculationTwoId ?? NewRandomString(),
            };
        }
    }
}