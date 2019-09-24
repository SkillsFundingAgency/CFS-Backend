using System.Collections.Generic;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class GeneratedProviderResultBuilder : TestEntityBuilder
    {
        private IEnumerable<FundingLine> _fundingLines;
        private IEnumerable<FundingCalculation> _fundingCalculations;
        private Provider _provider;

        public GeneratedProviderResultBuilder WithProvider(Provider provider)
        {
            _provider = provider;

            return this;
        }

        public GeneratedProviderResultBuilder WithFundlines(params FundingLine[] fundingLines)
        {
            _fundingLines = fundingLines;

            return this;
        }

        public GeneratedProviderResultBuilder WithFundingCalculations(params FundingCalculation[] calculations)
        {
            _fundingCalculations = calculations;

            return this;
        }

        public GeneratedProviderResult Build()
        {
            return new GeneratedProviderResult
            {
                Provider = _provider,
                Calculations = _fundingCalculations,
                FundingLines = _fundingLines
            };
        }
    }
}