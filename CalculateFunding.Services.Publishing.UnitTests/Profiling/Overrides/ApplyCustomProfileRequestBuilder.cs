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
        private string _fundingLineCode;
        private string _specificationId;
        private string _customProfileName;
        private decimal? _carryOver;
        private IEnumerable<ProfilePeriod> _profilePeriods;

        public ApplyCustomProfileRequestBuilder WithProfilePeriods(params ProfilePeriod[] profilePeriods)
        {
            _profilePeriods = profilePeriods;

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

        public ApplyCustomProfileRequestBuilder WithFundingLineCode(string fundingLineCode)
        {
            _fundingLineCode = fundingLineCode;

            return this;
        }

        public ApplyCustomProfileRequestBuilder WithCustomProfileName(string customProfileName)
        {
            _customProfileName = customProfileName;

            return this;
        }

        public ApplyCustomProfileRequestBuilder WithCarryOver(decimal? carryOver)
        {
            _carryOver = carryOver;

            return this;
        }

        public ApplyCustomProfileRequestBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public ApplyCustomProfileRequest Build()
        {
            return new ApplyCustomProfileRequest
            {
                ProviderId    = _providerId ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                FundingLineCode = _fundingLineCode ?? NewRandomString(),
                CustomProfileName = _customProfileName ?? NewRandomString(),
                SpecificationId = _specificationId ?? NewRandomString(),
                CarryOver = _carryOver,
                ProfilePeriods = _profilePeriods
            };
        }
    }
}