using CalculateFunding.Common.ApiClient.Policies.Models.ViewModels;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class FundingConfigurationUpdateViewModelBuilder : TestEntityBuilder
    {
        private string _defaultTemplateVersion;
        private ApprovalMode? _approvalMode;
        private UpdateCoreProviderVersion? _updateCoreProviderVersion;
        private IEnumerable<string> _errorDetectors;
        private IEnumerable<string> _allowedPublishedFundingStreamsIdsToReference;

        public FundingConfigurationUpdateViewModelBuilder WithApprovalMode(ApprovalMode approvalMode)
        {
            _approvalMode = approvalMode;

            return this;
        }
        
        public FundingConfigurationUpdateViewModelBuilder WithDefaultTemplateVersion(string defaultTemplateVersion)
        {
            _defaultTemplateVersion = defaultTemplateVersion;

            return this;
        }

        public FundingConfigurationUpdateViewModelBuilder WithUpdateCoreProviderVersion(UpdateCoreProviderVersion? updateCoreProviderVersion)
        {
            _updateCoreProviderVersion = updateCoreProviderVersion;

            return this;
        }

        public FundingConfigurationUpdateViewModelBuilder WithAllowedPublishedFundingStreamsIdsToReference(params string[] allowedPublishedFundingStreamsIdsToReference)
        {
            _allowedPublishedFundingStreamsIdsToReference = allowedPublishedFundingStreamsIdsToReference;
            return this;
        }

        public FundingConfigurationUpdateViewModel Build()
        {
            return new FundingConfigurationUpdateViewModel
            {
                DefaultTemplateVersion = _defaultTemplateVersion,
                ApprovalMode = _approvalMode.GetValueOrDefault(NewRandomEnum(ApprovalMode.Undefined)),
                ErrorDetectors = _errorDetectors,
                UpdateCoreProviderVersion = _updateCoreProviderVersion.GetValueOrDefault(NewRandomEnum(UpdateCoreProviderVersion.Manual)),
                AllowedPublishedFundingStreamsIdsToReference = _allowedPublishedFundingStreamsIdsToReference
            };
        }
    }
}
