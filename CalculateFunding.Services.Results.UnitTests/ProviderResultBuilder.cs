using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results.UnitTests
{
    public class ProviderResultBuilder : TestEntityBuilder
    {
        private ProviderSummary _providerSummary;
        private IEnumerable<CalculationResult> _calculationResults;
        private IEnumerable<FundingLineResult> _fundingLineResults;

        public ProviderResultBuilder WithProviderSummary(ProviderSummary providerSummary)
        {
            _providerSummary = providerSummary;

            return this;
        }

        public ProviderResultBuilder WithCalculationResults(params CalculationResult[] calculationResults)
        {
            _calculationResults = calculationResults;

            return this;
        }

        public ProviderResultBuilder WithFundingLineResults(params FundingLineResult[] fundingLineResults)
        {
            _fundingLineResults = fundingLineResults;

            return this;
        }

        public ProviderResult Build()
        {
            return new ProviderResult
            {
                Provider = _providerSummary,
                CalculationResults =  _calculationResults?.ToList() ?? new List<CalculationResult>(),
                FundingLineResults = _fundingLineResults?.ToList() ?? new List<FundingLineResult>()
            };
        }
    }
}