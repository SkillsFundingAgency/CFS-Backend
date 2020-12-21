using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class FundingConfigurationBuilder : TestEntityBuilder
    {
        private string _defaultTemplateVersion;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _id;
        private ApprovalMode? _approvalMode;
        private bool? _enableUserEditableRuleBasedProfiles;
        private bool? _enableUserEditableCustomProfiles;

        public FundingConfigurationBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

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

        public FundingConfigurationBuilder WithApprovalMode(ApprovalMode approvalMode)
        {
            _approvalMode = approvalMode;

            return this;
        }

        public FundingConfigurationBuilder WithEnableUserEditableCustomProfiles(bool? enableUserEditableCustomProfiles)
        {
            _enableUserEditableCustomProfiles = enableUserEditableCustomProfiles;

            return this;
        }

        public FundingConfigurationBuilder WithEnableUserEditableRuleBasedProfiles(bool? enableUserEditableRuleBasedProfiles)
        {
            _enableUserEditableRuleBasedProfiles = enableUserEditableRuleBasedProfiles;

            return this;
        }


        public FundingConfiguration Build()
        {
            return new FundingConfiguration
            {
                Id = _id ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                ApprovalMode = _approvalMode ?? NewRandomEnum<ApprovalMode>(),
                DefaultTemplateVersion = _defaultTemplateVersion,
                EnableUserEditableCustomProfiles = _enableUserEditableCustomProfiles ?? NewRandomFlag(),
                EnableUserEditableRuleBasedProfiles = _enableUserEditableRuleBasedProfiles ?? NewRandomFlag()
            };
        }
    }
}