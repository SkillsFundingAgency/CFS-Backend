using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Compiler.UnitTests
{
    public class PublishedSpecificationConfigurationBuilder : TestEntityBuilder
    {
        private string _specificationId;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private PublishedSpecificationItem[] _fundingLines;
        private PublishedSpecificationItem[] _calculations;

        public PublishedSpecificationConfigurationBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public PublishedSpecificationConfigurationBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public PublishedSpecificationConfigurationBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public PublishedSpecificationConfigurationBuilder WithFundingLines(params PublishedSpecificationItem[] fundingLines)
        {
            _fundingLines = fundingLines;

            return this;
        }

        public PublishedSpecificationConfigurationBuilder WithCalculations(params PublishedSpecificationItem[] calculations)
        {
            _calculations = calculations;

            return this;
        }

        public PublishedSpecificationConfiguration Build()
        {
            return new PublishedSpecificationConfiguration()
            {
                SpecificationId = _specificationId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingLines = _fundingLines,
                Calculations = _calculations
            };
        }
    }
}
