using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class FundingConfigurationParametersBuilder : TestEntityBuilder
    {
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _defaultTemplateVersion;
        private IEnumerable<string> _allowedPublishedFundingStreamsIdsToReference;

        public FundingConfigurationParametersBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;
            return this;
        }

        public FundingConfigurationParametersBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;
            return this;
        }

        public FundingConfigurationParametersBuilder WithDefaultTemplateVersion(string defaultTemplateVersion)
        {
            _defaultTemplateVersion = defaultTemplateVersion;
            return this;
        }

        public FundingConfigurationParametersBuilder WithAllowedPublishedFundingStreamsIdsToReference(params string[] allowedPublishedFundingStreamsIdsToReference)
        {
            _allowedPublishedFundingStreamsIdsToReference = allowedPublishedFundingStreamsIdsToReference;
            return this;
        }

        public FundingConfigurationParameters Build()
        {
            return new FundingConfigurationParameters()
            {
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                DefaultTemplateVersion = _defaultTemplateVersion ?? NewRandomString(),
                AllowedPublishedFundingStreamsIdsToReference = _allowedPublishedFundingStreamsIdsToReference ?? new string[0]
            };
        }
    }
}
