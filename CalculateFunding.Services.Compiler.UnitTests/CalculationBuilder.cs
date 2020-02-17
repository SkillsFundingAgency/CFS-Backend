using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Compiler.UnitTests
{
    public class CalculationBuilder : TestEntityBuilder
    {
        private string _id;
        private CalculationVersion _calculationVersion;

        public CalculationBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public CalculationBuilder WithCurrentVersion(CalculationVersion calculationVersion)
        {
            _calculationVersion = calculationVersion;

            return this;
        }
        
        public Calculation Build()
        {
            return new Calculation
            {
                Id = _id ?? NewRandomString(),
                Current = _calculationVersion
            };
        }
    }
}