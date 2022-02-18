using CalculateFunding.Common.ApiClient.Policies.Models.ViewModels;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class FundingConfigurationUpdateViewModelBuilder : TestEntityBuilder
    {
        private string _defaultTemplateVersion;
        private string _specToSpecChannelCode;
        private ApprovalMode? _approvalMode;
        private UpdateCoreProviderVersion? _updateCoreProviderVersion;
        private IEnumerable<string> _errorDetectors;
        private IEnumerable<string> _allowedPublishedFundingStreamsIdsToReference;
        private IEnumerable<FundingVariation> _releaseManagementVariations;
        private IEnumerable<FundingConfigurationChannel> _releaseChannels;
        private IEnumerable<ReleaseActionGroup> _releaseActionGroups;


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

        public FundingConfigurationUpdateViewModelBuilder WithSpecToSpecChannelCode(string specToSpecChannelCode)
        {
            _specToSpecChannelCode = specToSpecChannelCode;

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

        public FundingConfigurationUpdateViewModelBuilder WithReleaseManagementVariations(params FundingVariation[] releaseManagementVariations)
        {
            _releaseManagementVariations = releaseManagementVariations;
            return this;
        }

        public FundingConfigurationUpdateViewModelBuilder WithReleaseChannels(params FundingConfigurationChannel[] releaseChannels)
        {
            _releaseChannels = releaseChannels;
            return this;
        }

        public FundingConfigurationUpdateViewModelBuilder WithReleaseActionGroups(params ReleaseActionGroup[] releaseActionGroups)
        {
            _releaseActionGroups = releaseActionGroups;
            return this;
        }

        public FundingConfigurationUpdateViewModel Build()
        {
            return new FundingConfigurationUpdateViewModel
            {
                DefaultTemplateVersion = _defaultTemplateVersion,
                SpecToSpecChannelCode = _specToSpecChannelCode,
                ApprovalMode = _approvalMode.GetValueOrDefault(NewRandomEnum(ApprovalMode.Undefined)),
                ErrorDetectors = _errorDetectors,
                UpdateCoreProviderVersion = _updateCoreProviderVersion.GetValueOrDefault(NewRandomEnum(UpdateCoreProviderVersion.Manual)),
                AllowedPublishedFundingStreamsIdsToReference = _allowedPublishedFundingStreamsIdsToReference,
                ReleaseManagementVariations = _releaseManagementVariations,
                ReleaseChannels = _releaseChannels,
                ReleaseActionGroups = _releaseActionGroups
            };
        }
    }
}
