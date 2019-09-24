using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class FundingCalculationBuilder : TestEntityBuilder
    {
        private uint? _templateCalculationId;
        private object _value;

        public FundingCalculationBuilder WithTemplateCalculationId(uint templateCalculationId)
        {
            _templateCalculationId = templateCalculationId;

            return this;
        }

        public FundingCalculationBuilder WithValue(object value)
        {
            _value = value;

            return this;
        }
        
        public FundingCalculation Build()
        {
            return new FundingCalculation
            {
                Value = _value,
                TemplateCalculationId = _templateCalculationId.GetValueOrDefault((uint)NewRandomNumberBetween(1, int.MaxValue))
            };
        }
    }
}