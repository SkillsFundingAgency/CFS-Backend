using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing
{
    public class CalculationResultBuilder : TestEntityBuilder
    {
        private string _calculationId;
        private decimal? _value;

        public CalculationResultBuilder WithCalculation(string calculationId)
        {
            _calculationId = calculationId;

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
                Id = _calculationId ?? new RandomString(),
                Value = _value
            };
        }
    }
}