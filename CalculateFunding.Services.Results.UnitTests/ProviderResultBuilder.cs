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
        private string _specificationId;
        private bool _isIndicativeProvider;

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

        public ProviderResultBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public ProviderResultBuilder WithIsIndicativeProvider(bool isIndicativeProvider)
        {
            _isIndicativeProvider = isIndicativeProvider;

            return this;
        }

        public ProviderResult Build()
        {
            return new ProviderResult
            {
                SpecificationId = _specificationId,
                Provider = _providerSummary,
                CalculationResults =  _calculationResults?.ToList() ?? new List<CalculationResult>(),
                FundingLineResults = _fundingLineResults?.ToList() ?? new List<FundingLineResult>(),
                IsIndicativeProvider = _isIndicativeProvider
            };
        }
    }
}