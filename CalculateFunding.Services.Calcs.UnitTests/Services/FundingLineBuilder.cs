using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.Services
{
    public class FundingLineBuilder : TestEntityBuilder
    {
        private IEnumerable<Calculation> _calculations = Enumerable.Empty<Calculation>();
        private IEnumerable<FundingLine> _fundingLines = Enumerable.Empty<FundingLine>();
        private string _name;
        private uint? _templateId;

        public FundingLineBuilder WithTemplateId(uint templateId)
        {
            _templateId = templateId;

            return this;
        }

        public FundingLineBuilder WithName(string name)
        {
            _name = name;

            return this;
        }
        
        public FundingLineBuilder WithCalculations(params Calculation[] calculations)
        {
            _calculations = calculations;

            return this;
        }
        
        public FundingLineBuilder WithFundingLines(params FundingLine[] fundingLines)
        {
            _fundingLines = fundingLines;

            return this;
        }
        
        public FundingLine Build()
        {
            return new FundingLine
            {
                TemplateLineId = _templateId.GetValueOrDefault((uint) NewRandomNumberBetween(1, int.MaxValue)),
                Name = _name ?? NewRandomString(),
                Calculations = _calculations.ToArray(),
                FundingLines = _fundingLines.ToArray()
            };
        }
    }
}