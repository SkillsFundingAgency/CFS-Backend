using System;
using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Profiling.IntegrationTests.ReProfiling
{
    public class ReProfileRequestBuilder : TestEntityBuilder
    {
        private string _fundingLineCode;
        private string _fundingPeriodId;
        private string _fundingStreamId;
        private decimal? _fundingValue;
        private decimal? _existingFundingLineTotal;
        private string _profilePatternKey;
        private bool _midYear;
        private IEnumerable<ExistingProfilePeriod> _existingProfilePeriods;
        private ProfileConfigurationType? _configurationType;
        private int? _variationPointerIndex;
        private bool _noProfilePatternKey;

        public ReProfileRequestBuilder WithVariationPointerIndex(int? variationPointerIndex)
        {
            _variationPointerIndex = variationPointerIndex;

            return this;
        }
        
        public ReProfileRequestBuilder WithConfigurationType(ProfileConfigurationType configurationType)
        {
            _configurationType = configurationType;

            return this;
        }

        public ReProfileRequestBuilder WithExistingProfilePeriods(params ExistingProfilePeriod[] existingProfilePeriods)
        {
            _existingProfilePeriods = existingProfilePeriods;

            return this;
        }
        
        public ReProfileRequestBuilder WithMidYear(bool midYear)
        {
            _midYear = midYear;

            return this;
        }

        public ReProfileRequestBuilder WithFundingLineCode(string fundingLineCode)
        {
            _fundingLineCode = fundingLineCode;

            return this;
        }

        public ReProfileRequestBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public ReProfileRequestBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public ReProfileRequestBuilder WithExistingFundingValue(decimal fundingValue)
        {
            _existingFundingLineTotal = fundingValue;

            return this;
        }

        public ReProfileRequestBuilder WithFundingValue(decimal fundingValue)
        {
            _fundingValue = fundingValue;

            return this;
        }

        public ReProfileRequestBuilder WithProfilePatternKey(string profilePatternKey)
        {
            _profilePatternKey = profilePatternKey;

            return this;
        }

        public ReProfileRequestBuilder WithNoProfilePatternKey()
        {
            _noProfilePatternKey = true;

            return this;
        }
        
        public ReProfileRequest Build()
        {
            return new ReProfileRequest
            {
                ProfilePatternKey = _noProfilePatternKey ? null : _profilePatternKey ?? NewRandomString(),
                FundingLineCode = _fundingLineCode ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingLineTotal = _fundingValue.GetValueOrDefault(NewRandomNumberBetween(999, int.MaxValue)),
                ExistingFundingLineTotal = _existingFundingLineTotal.GetValueOrDefault(NewRandomNumberBetween(999, int.MaxValue)),
                MidYear = _midYear,
                ExistingPeriods = _existingProfilePeriods ?? ArraySegment<ExistingProfilePeriod>.Empty,
                ConfigurationType = _configurationType.GetValueOrDefault(NewRandomEnum<ProfileConfigurationType>()),
                VariationPointerIndex = _variationPointerIndex
            };
        }
    }
}