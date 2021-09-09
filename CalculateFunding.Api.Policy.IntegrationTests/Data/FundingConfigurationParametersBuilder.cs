﻿using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class FundingConfigurationParametersBuilder : TestEntityBuilder
    {
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _defaultTemplateVersion;
        private IEnumerable<string> _allowedPublishedFundingStreamsIdsToReference;
        private IEnumerable<FundingVariation> _releaseManagementVariationTypes;
        private IEnumerable<FundingConfigurationChannel> _releaseChannels;

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

        public FundingConfigurationParametersBuilder WithReleaseManagementVariations(
            params FundingVariation[] releaseManagementVariationTypes)
        {
            _releaseManagementVariationTypes = releaseManagementVariationTypes;
            return this;
        }

        public FundingConfigurationParametersBuilder WithReleaseChannels(
            params FundingConfigurationChannel[] releaseChannels)
        {
            _releaseChannels = releaseChannels;
            return this;
        }

        public FundingConfigurationParameters Build()
            => new FundingConfigurationParameters()
            {
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                DefaultTemplateVersion = _defaultTemplateVersion ?? NewRandomString(),
                AllowedPublishedFundingStreamsIdsToReference = _allowedPublishedFundingStreamsIdsToReference ?? Array.Empty<string>(),
                ReleaseManagementVariations = _releaseManagementVariationTypes ?? Array.Empty<FundingVariation>(),
                ReleaseChannels = _releaseChannels ?? Array.Empty<FundingConfigurationChannel>()
            };
    }
}
