using CalculateFunding.Models.Policy.FundingPolicy;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Policy.Validators
{
    public class FundingConfigurationBuilder : TestEntityBuilder
    {
        private string _defaultTemplateVersion;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private ApprovalMode? _approvalMode;
        
        public FundingConfigurationBuilder WithApprovalMode(ApprovalMode approvalMode)
        {
            _approvalMode = approvalMode;

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

        public FundingConfiguration Build()
        {
            return new FundingConfiguration
            {
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                DefaultTemplateVersion = _defaultTemplateVersion,
                ApprovalMode = _approvalMode.GetValueOrDefault(NewRandomEnum(ApprovalMode.Undefined))
            };
        }
    }
}