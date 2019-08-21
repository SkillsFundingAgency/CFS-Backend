using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Calcs.Services
{
    public class TemplateMappingCalculationBuilder : TestEntityBuilder
    {
        private string _name;
        private CalculationValueFormat? _valueFormat;
        private uint _templateCalculationId;
        private IEnumerable<Calculation> _calculations = Enumerable.Empty<Calculation>();

        public TemplateMappingCalculationBuilder WithCalculations(params Calculation[] calculations)
        {
            _calculations = calculations;

            return this;
        }

        public TemplateMappingCalculationBuilder WithTemplateCalculationId(uint templateId)
        {
            _templateCalculationId = templateId;

            return this;
        }

        public TemplateMappingCalculationBuilder WithValueFormat(CalculationValueFormat valueFormat)
        {
            _valueFormat = valueFormat;

            return this;
        }

        public TemplateMappingCalculationBuilder WithName(string name)
        {
            _name = name;

            return this;
        }
        
        public Calculation Build()
        {
            return new Calculation
            {
                Name = _name ?? NewRandomString(),
                ValueFormat = _valueFormat.GetValueOrDefault(NewRandomEnum<CalculationValueFormat>()),
                TemplateCalculationId = _templateCalculationId,
                Calculations = _calculations
            };
        }
    }
}