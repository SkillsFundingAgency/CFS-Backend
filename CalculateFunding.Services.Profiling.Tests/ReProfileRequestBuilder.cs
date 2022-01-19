using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;
using System.Collections.Generic;

namespace CalculateFunding.Services.Profiling.Tests
{
    public class ReProfileRequestBuilder : TestEntityBuilder
    {
        private string _fundingLineCode;
        private string _fundingPeriodId;
        private string _fundingStreamId;
        private decimal? _fundingValue;
        private decimal? _existingFundingLineTotal;
        private string _forceSameAsKey;
        private IEnumerable<ExistingProfilePeriod> _existingProfilePeriods;
        private string _profilePatternKey;
        private int? _variationPointer;
        private MidYearType? _midYearType;
        
        public ReProfileRequestBuilder WithMidYearCatchup(MidYearType? midYearType)
        {
            _midYearType = midYearType;

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

        public ReProfileRequestBuilder WithExistingProfilePeriods(IEnumerable<ExistingProfilePeriod> existingProfilePeriods)
        {
            _existingProfilePeriods = existingProfilePeriods;

            return this;
        }

        public ReProfileRequestBuilder WithProfilePatternKey(string profilePatternKey)
        {
            _profilePatternKey = profilePatternKey;

            return this;
        }

        public ReProfileRequestBuilder WithVariationPointer(int variationPointer)
        {
            _variationPointer = variationPointer;

            return this;
        }

        public ReProfileRequestBuilder WithForceSameAsKey(string forceSameAsKey)
        {
            _forceSameAsKey = forceSameAsKey;

            return this;
        }

        public ReProfileRequest Build()
        {
            return new ReProfileRequest
            {
                ProfilePatternKey = _profilePatternKey ?? NewRandomString(),
                FundingLineCode = _fundingLineCode ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingLineTotal = _fundingValue.GetValueOrDefault(NewRandomNumberBetween(999, int.MaxValue)),
                ExistingFundingLineTotal = _existingFundingLineTotal.GetValueOrDefault(NewRandomNumberBetween(999, int.MaxValue)),
                ExistingPeriods = _existingProfilePeriods,
                VariationPointerIndex = _variationPointer,
                MidYearType = _midYearType,
                ForceSameAsKey = _forceSameAsKey
            };
        }
        
    }
}