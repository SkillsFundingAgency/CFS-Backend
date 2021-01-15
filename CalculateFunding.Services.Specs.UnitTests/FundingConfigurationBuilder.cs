using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class FundingConfigurationBuilder : TestEntityBuilder
    {
        private string _defaultTemplateVersion;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private ProviderSource _providerSource;
        private bool _runCalculationEngineAfterCoreProviderUpdate;

        public FundingConfigurationBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public FundingConfigurationBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public FundingConfigurationBuilder WithDefaultTemplateVersion(string defaultTemplateVersion)
        {
            _defaultTemplateVersion = defaultTemplateVersion;

            return this;
        }

        public FundingConfigurationBuilder WithProviderSource(ProviderSource providerSource)
        {
            _providerSource = providerSource;

            return this;
        }

        public FundingConfigurationBuilder WithRunCalculationEngineAfterCoreProviderUpdate(bool runCalculationEngineAfterCoreProviderUpdate)
        {
            _runCalculationEngineAfterCoreProviderUpdate = runCalculationEngineAfterCoreProviderUpdate;

            return this;
        }

        public FundingConfiguration Build()
        {
            return new FundingConfiguration
            {
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                DefaultTemplateVersion = _defaultTemplateVersion,
                ProviderSource = _providerSource,
                RunCalculationEngineAfterCoreProviderUpdate = _runCalculationEngineAfterCoreProviderUpdate
            };
        }
    }
}