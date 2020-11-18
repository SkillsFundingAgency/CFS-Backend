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
        private string _name;
        private string _fundingLineCode;

        public TemplateFundingLineBuilder WithFundingLineCode(string fundingLineCode)
        {
            _fundingLineCode = fundingLineCode;

            return this;
        }

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

        public TemplateFundingLineBuilder WithName(string name)
        {
            _name = name;

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
                Name = _name,
                TemplateLineId = _lineId.GetValueOrDefault((uint)NewRandomNumberBetween(1, int.MaxValue)),
                Calculations = _calculations ?? new Calculation[0],
                FundingLines = _fundingLines ?? new FundingLine[0],
                FundingLineCode = _fundingLineCode ?? NewRandomString()
            };
        }
    }
}