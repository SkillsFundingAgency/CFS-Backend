using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Tests.Common.Helpers;
using System;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Datasets
{
    public class FundingConfigurationTemplateParametersBuilder : TestEntityBuilder
    {
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _defaultTemplateVersion;
        private ProviderSource? _providerSource;
        private bool? _enableConverterDataMerge;
        private string[] _indicativeOpenerProviderStatus;
        private string[] _allowedPublishedFundingStreamsIdsToReference;

        public FundingConfigurationTemplateParametersBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public FundingConfigurationTemplateParametersBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public FundingConfigurationTemplateParametersBuilder WithDefaultTemplateVersion(string defaultTemplateVersion)
        {
            _defaultTemplateVersion = defaultTemplateVersion;

            return this;
        }

        public FundingConfigurationTemplateParametersBuilder WithProviderSource(ProviderSource providerSource)
        {
            _providerSource = providerSource;

            return this;
        }

        public FundingConfigurationTemplateParametersBuilder WithEnableConverterDataMerge(bool enableConverterDataMerge)
        {
            _enableConverterDataMerge = enableConverterDataMerge;

            return this;
        }

        public FundingConfigurationTemplateParametersBuilder WithIndicativeOpenerProviderStatus(params string[] indicativeOpenerProviderStatus)
        {
            _indicativeOpenerProviderStatus = indicativeOpenerProviderStatus;

            return this;
        }

        public FundingConfigurationTemplateParametersBuilder WithAllowedPublishedFundingStreamsIdsToReference(params string[] allowedPublishedFundingStreamsIdsToReference)
        {
            _allowedPublishedFundingStreamsIdsToReference = allowedPublishedFundingStreamsIdsToReference;

            return this;
        }

        public FundingConfigurationTemplateParameters Build() =>
            new FundingConfigurationTemplateParameters
            {
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                DefaultTemplateVersion = _defaultTemplateVersion ?? NewRandomString(),
                IndicativeOpenerProviderStatus = _indicativeOpenerProviderStatus ?? Array.Empty<string>(),
                EnableConverterDataMerge = _enableConverterDataMerge.GetValueOrDefault(NewRandomFlag()),
                ProviderSource = _providerSource.GetValueOrDefault(NewRandomEnum<ProviderSource>()),
                AllowedPublishedFundingStreamsIdsToReference = _allowedPublishedFundingStreamsIdsToReference ?? Array.Empty<string>()
            };
    }
}