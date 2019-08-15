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
                Calculations = _calculations.ToArray(),
                FundingLines = _fundingLines.ToArray()
            };
        }
    }
}