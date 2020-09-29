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
        private CalculationType? _type;
        private AggregationType? _aggregationType;
        private string _formulaText;
        private uint? _templateCalculationId;
        private IEnumerable<Calculation> _calculations = null;
        private IEnumerable<string> _allowedEnumTypeValues;

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

        public TemplateMappingCalculationBuilder WithType(CalculationType type)
        {
            _type = type;

            return this;
        }

        public TemplateMappingCalculationBuilder WithAggregationType(AggregationType aggregationType)
        {
            _aggregationType = aggregationType;

            return this;
        }

        public TemplateMappingCalculationBuilder WithFormulaText(string formulaText)
        {
            _formulaText = formulaText;

            return this;
        }

        public TemplateMappingCalculationBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public TemplateMappingCalculationBuilder WithAllowedEnumTypeValues(IEnumerable<string> allowedEnumTypeValues)
        {
            _allowedEnumTypeValues = allowedEnumTypeValues;

            return this;
        }

        public Calculation Build()
        {
            return new Calculation
            {
                Name = _name ?? NewRandomString(),
                ValueFormat = _valueFormat.GetValueOrDefault(NewRandomEnum<CalculationValueFormat>()),
                Type = _type.GetValueOrDefault(CalculationType.Cash),
                FormulaText = _formulaText,
                AggregationType = _aggregationType.GetValueOrDefault(AggregationType.Sum),
                TemplateCalculationId = _templateCalculationId ?? NewRandomUint(),
                Calculations = _calculations,
                AllowedEnumTypeValues = _allowedEnumTypeValues
            };
        }
    }
}