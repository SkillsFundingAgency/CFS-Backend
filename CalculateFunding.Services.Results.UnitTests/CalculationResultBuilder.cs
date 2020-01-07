using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results
{
    public class CalculationResultBuilder : TestEntityBuilder
    {
        private Reference _calculation;
        private CalculationType? _calculationType;
        private decimal? _value;

        public CalculationResultBuilder WithCalculation(Reference calculation)
        {
            _calculation = calculation;

            return this;
        }
        
        public CalculationResultBuilder WithCalculationType(CalculationType calculationType)
        {
            _calculationType = calculationType;

            return this;
        }
        
        public CalculationResultBuilder WithValue(decimal? value)
        {
            _value = value;

            return this;
        }
        
        public CalculationResult Build()
        {
            return new CalculationResult
            {
                Calculation = _calculation,
                Value = _value,
                CalculationType = _calculationType.GetValueOrDefault(NewRandomEnum<CalculationType>())
            };
        }
    }
}