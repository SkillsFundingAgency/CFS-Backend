using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Results;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results
{
    public class ProviderResultBuilder : TestEntityBuilder
    {
        private ProviderSummary _providerSummary;
        private IEnumerable<CalculationResult> _calculationResults;
        
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
        
        public ProviderResult Build()
        {
            return new ProviderResult
            {
                Provider = _providerSummary,
                CalculationResults =  _calculationResults?.ToList() ?? new List<CalculationResult>()
            };
        }
    }
}