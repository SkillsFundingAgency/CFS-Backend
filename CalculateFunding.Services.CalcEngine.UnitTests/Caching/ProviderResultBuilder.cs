using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calculator.Caching
{
    public class ProviderResultBuilder : TestEntityBuilder
    {
        private string _specificationId;
        private string _providerId;
        private IEnumerable<CalculationResult> _calculationResults;
        private IEnumerable<FundingLineResult> _fundingLineResults;

        public ProviderResultBuilder WithProviderId(string id)
        {
            _providerId = id;

            return this;
        }

        public ProviderResultBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public ProviderResultBuilder WithCalculationResults(params CalculationResult[] results)
        {
            _calculationResults = results;

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
                Provider = new ProviderSummary
                {
                    Id = _providerId ?? NewRandomString()
                },
                SpecificationId = _specificationId ?? NewRandomString(),
                CalculationResults = _calculationResults?.ToList() ?? new List<CalculationResult>(),
                FundingLineResults = _fundingLineResults?.ToList() ?? new List<FundingLineResult>()
            };
        }
    }
}