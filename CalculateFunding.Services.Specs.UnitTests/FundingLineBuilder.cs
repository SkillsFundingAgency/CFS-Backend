using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class FundingLineBuilder : TestEntityBuilder
    {
        private IEnumerable<Calculation> _calculations = Enumerable.Empty<Calculation>();

        public FundingLineBuilder WithCalculations(params Calculation[] calculations)
        {
            _calculations = calculations;

            return this;
        }
        
        public FundingLine Build()
        {
            return new FundingLine
            {
                Calculations = _calculations.ToArray()
            };
        }
    }
}