using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Calcs.UnitTests.Services
{
    public class FundingConfigurationBuilder : TestEntityBuilder
    {
        private string _defaultTemplateVersion;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private ApprovalMode? _approvalMode;
        private ProviderSource? _providerSource;
        private PaymentOrganisationSource? _paymentOrganisationSource;
        private UpdateCoreProviderVersion? _updateCoreProviderVersion;
        private IEnumerable<string> _errorDetectors;
        private IEnumerable<string> _allowedPublishedFundingStreamsIdsToReference;
        private string[] _indicativeOpenerProviderStatus;

        public FundingConfigurationBuilder WithApprovalMode(ApprovalMode approvalMode)
        {
            _approvalMode = approvalMode;

            return this;
        }

        public FundingConfigurationBuilder WithErrorDetectors(params string[] errorDetectors)
        {
            _errorDetectors = errorDetectors;

            return this;
        }

        public FundingConfigurationBuilder WithProviderSource(ProviderSource providerSource)
        {
            _providerSource = providerSource;

            return this;
        }

        public FundingConfigurationBuilder WithPaymentOrganisationSource(PaymentOrganisationSource paymentOrganisationSource)
        {
            _paymentOrganisationSource = paymentOrganisationSource;

            return this;
        }

        public FundingConfigurationBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public FundingConfigurationBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public FundingConfigurationBuilder WithDefaultTemplateVersion(string defaultTemplateVersion)
        {
            _defaultTemplateVersion = defaultTemplateVersion;

            return this;
        }

        public FundingConfigurationBuilder WithUpdateCoreProviderVersion(UpdateCoreProviderVersion? updateCoreProviderVersion)
        {
            _updateCoreProviderVersion = updateCoreProviderVersion;

            return this;
        }

        public FundingConfigurationBuilder WithAllowedPublishedFundingStreamsIdsToReference(params string[] allowedPublishedFundingStreamsIdsToReference)
        {
            _allowedPublishedFundingStreamsIdsToReference = allowedPublishedFundingStreamsIdsToReference;
            return this;
        }

        public FundingConfigurationBuilder WithIndicativeOpenerProviderStatus(params string[] indicativeOpenerProviderStatus)
        {
            _indicativeOpenerProviderStatus = indicativeOpenerProviderStatus;
            return this;
        }

        public FundingConfiguration Build()
        {
            return new FundingConfiguration
            {
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                DefaultTemplateVersion = _defaultTemplateVersion,
                ApprovalMode = _approvalMode.GetValueOrDefault(NewRandomEnum(ApprovalMode.Undefined)),
                ErrorDetectors = _errorDetectors,
                ProviderSource = _providerSource.GetValueOrDefault(NewRandomEnum(ProviderSource.CFS)),
                PaymentOrganisationSource = _paymentOrganisationSource.GetValueOrDefault(NewRandomEnum(PaymentOrganisationSource.PaymentOrganisationAsProvider)),
                UpdateCoreProviderVersion = _updateCoreProviderVersion.GetValueOrDefault(NewRandomEnum(UpdateCoreProviderVersion.Manual)),
                AllowedPublishedFundingStreamsIdsToReference = _allowedPublishedFundingStreamsIdsToReference,
                IndicativeOpenerProviderStatus= _indicativeOpenerProviderStatus
            };
        }
    }
}
