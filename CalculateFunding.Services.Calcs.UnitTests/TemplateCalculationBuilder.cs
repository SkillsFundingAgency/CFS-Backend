using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.UnitTests
{
    public class TemplateCalculationBuilder : TestEntityBuilder
    {
        private uint? _templateCalculationId;
        private IEnumerable<Calculation> _calculations;

        public TemplateCalculationBuilder WithTemplateCalculationId(uint templateCalculationId)
        {
            _templateCalculationId = templateCalculationId;

            return this;
        }

        public TemplateCalculationBuilder WithCalculations(params Calculation[] calculations)
        {
            _calculations = calculations;

            return this;
        }
        
        public Calculation Build()
        {
            return new Calculation
            {
                Calculations = _calculations,
                TemplateCalculationId = _templateCalculationId.GetValueOrDefault((uint) NewRandomNumberBetween(1, int.MaxValue))
            };
        }
    }
}