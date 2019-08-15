using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.Services
{
    public class TemplateMappingCalculationBuilder : TestEntityBuilder
    {
        private string _name;
        private CalculationValueFormat? _valueFormat;
        private uint _templateCalculationId;

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
                TemplateCalculationId = _templateCalculationId
            };
        }
    }
}