using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class TemplateCalcationBuilder : TestEntityBuilder
    {
        private uint? _templateCalculationId;
        private IEnumerable<Calculation> _calculations;

        public TemplateCalcationBuilder WithTemplateCalculationId(uint templateCalculationId)
        {
            _templateCalculationId = templateCalculationId;

            return this;
        }

        public TemplateCalcationBuilder WithCalculations(params Calculation[] calculations)
        {
            _calculations = calculations;

            return this;
        }
        
        public Calculation Build()
        {
            return new Calculation
            {
                Calculations = _calculations ?? new Calculation[0],
                TemplateCalculationId = _templateCalculationId.GetValueOrDefault((uint)NewRandomNumberBetween(1, int.MaxValue))
            };
        }
    }
}