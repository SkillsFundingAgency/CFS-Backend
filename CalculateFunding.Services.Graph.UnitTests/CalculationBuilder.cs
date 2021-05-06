using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Services.Graph.UnitTests
{
    public class CalculationBuilder : TestEntityBuilder
    {
        private string _calculationId;
        private string _calculationName;
        private CalculationType? _calculationType;
        private string _fundingStream;
        private string _specificationId;

        public CalculationBuilder WithCalculationId(string calculationId)
        {
            _calculationId = calculationId;

            return this;
        }
        public CalculationBuilder WithCalculationName(string calculationName)
        {
            _calculationName = calculationName;

            return this;
        }
        public CalculationBuilder WithCalculationType(CalculationType calculationType)
        {
            _calculationType = calculationType;

            return this;
        }
        public CalculationBuilder WithFundingStream(string fundingStream)
        {
            _fundingStream = fundingStream;

            return this;
        }
        public CalculationBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public Calculation Build()
        {
            return new Calculation
            {
                CalculationId = _calculationId ?? new RandomString(),
                CalculationName = _calculationName ?? new RandomString(),
                CalculationType = _calculationType ?? CalculationType.Template,
                FundingStream = _fundingStream ?? new RandomString(),
                SpecificationId = _specificationId ?? new RandomString()
            };
        }
    }
}