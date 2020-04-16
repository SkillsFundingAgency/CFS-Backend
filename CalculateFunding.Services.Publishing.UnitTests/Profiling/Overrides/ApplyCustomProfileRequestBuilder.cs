using System.Collections.Generic;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Profiling.Custom;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling.Overrides
{
    public class ApplyCustomProfileRequestBuilder : TestEntityBuilder
    {
        private string _providerId;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _customProfileName;
        private IEnumerable<FundingLineProfileOverrides> _overrides;

        public ApplyCustomProfileRequestBuilder WithProfileOverrides(params FundingLineProfileOverrides[] overrides)
        {
            _overrides = overrides;

            return this;
        }

        public ApplyCustomProfileRequestBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public ApplyCustomProfileRequestBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public ApplyCustomProfileRequestBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public ApplyCustomProfileRequestBuilder WithCustomProfileName(string customProfileName)
        {
            _customProfileName = customProfileName;

            return this;
        }
        
        public ApplyCustomProfileRequest Build()
        {
            return new ApplyCustomProfileRequest
            {
                ProviderId    = _providerId ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                CustomProfileName = _customProfileName ?? NewRandomString(),
                ProfileOverrides = _overrides
            };
        }
    }
}