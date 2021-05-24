using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class FundingTemplateParametersBuilder : TestEntityBuilder
    {
        private string _id;
        private string _fundingPeriodId;
        private string _fundingStreamId;
        private string _fundingStreamName;
        private string _fundingVersion;
        private string _templateVersion;

        public FundingTemplateParametersBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;
            return this;
        }

        public FundingTemplateParametersBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;
            return this;
        }

        public FundingTemplateParametersBuilder WithFundingStreamName(string fundingStreamName)
        {
            _fundingStreamName = fundingStreamName;
            return this;
        }

        public FundingTemplateParametersBuilder WithFundingVersion(string fundingVersion)
        {
            _fundingVersion = fundingVersion;
            return this;
        }

        public FundingTemplateParametersBuilder WithTemplateVersion(string templateVersion)
        {
            _templateVersion = templateVersion;
            return this;
        }

        public FundingTemplateParametersBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        public FundingTemplateParameters Build()
        {
            return new FundingTemplateParameters()
            {
                Id = _id ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                FundingStreamName = _fundingStreamName ?? NewRandomString(),
                FundingVersion = _fundingVersion ?? $"{NewRandomNumberBetween(1,10)}_{NewRandomNumberBetween(1, 10)}",
                TemplateVersion = _templateVersion ?? $"{NewRandomNumberBetween(1, 10)}.{NewRandomNumberBetween(1, 10)}"
            };
        }
    }
}
