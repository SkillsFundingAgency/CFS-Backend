using CalculateFunding.Tests.Common.Helpers;
using PolicyApiClientModel = CalculateFunding.Common.ApiClient.Policies.Models;


namespace CalculateFunding.Api.External.UnitTests.Version4
{
    public class PolicyFundingConfigurationBuilder : TestEntityBuilder
    {
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _defaultTemplateVersion;

        public PolicyFundingConfigurationBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;
            return this;
        }

        public PolicyFundingConfigurationBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;
            return this;
        }

        public PolicyFundingConfigurationBuilder WithDefaultTemplateVersion(string defaultTemplateVersion)
        {
            _defaultTemplateVersion = defaultTemplateVersion;
            return this;
        }

        public PolicyApiClientModel.FundingConfig.FundingConfiguration Build()
        {
            return new PolicyApiClientModel.FundingConfig.FundingConfiguration
            {
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                DefaultTemplateVersion = _defaultTemplateVersion ?? NewRandomString()
            };
        }
    }
}
