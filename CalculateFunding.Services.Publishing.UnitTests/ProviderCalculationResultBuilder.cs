using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ProviderCalculationResultBuilder : TestEntityBuilder
    {
        private string _providerId;
        private IEnumerable<CalculationResult> _calculationResults;

        public ProviderCalculationResultBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public ProviderCalculationResultBuilder WithResults(IEnumerable<CalculationResult> calculationResults)
        {
            _calculationResults = calculationResults;

            return this;
        }

        public ProviderCalculationResult Build()
        {
            return new ProviderCalculationResult
            {
                ProviderId = _providerId ?? NewRandomString(),
                Results = _calculationResults ?? new CalculationResult[0]
            };
        }
    }
}
