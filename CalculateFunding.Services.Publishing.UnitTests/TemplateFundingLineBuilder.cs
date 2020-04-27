using System;
using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class TemplateFundingLineBuilder : TestEntityBuilder
    {
        private uint? _lineId;
        private IEnumerable<Calculation> _calculations;
        private IEnumerable<FundingLine> _fundingLines;

        public TemplateFundingLineBuilder WithFundingLines(params FundingLine[] fundingLines)
        {
            _fundingLines = fundingLines;

            return this;
        }
        
        public TemplateFundingLineBuilder WithTemplateLineId(uint id)
        {
            _lineId = id;

            return this;
        }

        public TemplateFundingLineBuilder WithCalculations(params Calculation[] calculations)
        {
            _calculations = calculations;

            return this;
        }
        
        public FundingLine Build()
        {
            return new FundingLine
            {
                TemplateLineId = _lineId.GetValueOrDefault((uint)NewRandomNumberBetween(1, int.MaxValue)),
                Calculations = _calculations ?? new Calculation[0],
                FundingLines = _fundingLines ?? new FundingLine[0]
            };
        }
    }
}