using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.Services
{
    public class CalculationVersionBuilder : TestEntityBuilder
    {
        private string _name;
        private CalculationValueType _calculationValueType;

        public CalculationVersionBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public CalculationVersionBuilder WithValueType(CalculationValueType calculationValueType)
        {
            _calculationValueType = calculationValueType;

            return this;
        }

        public CalculationVersion Build()
        {
            return new CalculationVersion
            {
                Name = _name,
                ValueType = _calculationValueType
            };
        }
    }
}
