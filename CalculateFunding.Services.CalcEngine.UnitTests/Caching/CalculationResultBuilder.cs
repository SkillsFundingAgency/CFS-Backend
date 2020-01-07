using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calculator.Caching
{
    public class CalculationResultBuilder : TestEntityBuilder
    {
        private decimal? _value;
        private CalculationType? _calculationType;

        public CalculationResultBuilder WithValue(decimal value)
        {
            _value = value;

            return this;
        }

        public CalculationResultBuilder WithCalculationType(CalculationType calculationType)
        {
            _calculationType = calculationType;

            return this;
        }

        public CalculationResult Build()
        {
            return new CalculationResult
            {
                CalculationType = _calculationType.GetValueOrDefault(NewRandomEnum<CalculationType>()),
                Value = _value.GetValueOrDefault(NewRandomNumberBetween(0, int.MaxValue))
            };
        }
    }
}