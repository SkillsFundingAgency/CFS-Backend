using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class TemplateCalculationBuilder : TestEntityBuilder
    {
        private uint? _templateCalculationId;
        private IEnumerable<Calculation> _calculations;
        private CalculationType? _type;
        private CalculationValueFormat? _valueFormat;
        private string _name;

        public TemplateCalculationBuilder WithValueFormat(CalculationValueFormat valueFormat)
        {
            _valueFormat = valueFormat;

            return this;
        }
        
        public TemplateCalculationBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public TemplateCalculationBuilder WithType(CalculationType type)
        {
            _type = type;

            return this;
        }

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
                Calculations = _calculations ?? new Calculation[0],
                TemplateCalculationId = _templateCalculationId.GetValueOrDefault((uint)NewRandomNumberBetween(1, int.MaxValue)),
                ValueFormat = _valueFormat.GetValueOrDefault(NewRandomEnum<CalculationValueFormat>()),
                Type = _type.GetValueOrDefault(),
                Name = _name
            };
        }
    }
}