using System.Collections.Generic;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;

namespace CalculateFunding.Services.Profiling.Tests
{
    public class ProfileBatchRequestBuilder : TestEntityBuilder
    {
        private IEnumerable<decimal> _fundingValues;
        private string _fundingLineCode;
        private string _fundingPeriod;
        private string _providerType;
        private string _fundingStreamId;
        private string _profilePatternKey;
        private string _providerSubType;

        public ProfileBatchRequestBuilder WithFundingValues(params decimal[] fundingValues)
        {
            _fundingValues = fundingValues;

            return this;
        }

        public ProfileBatchRequestBuilder WithFundingLineCode(string fundingLineCode)
        {
            _fundingLineCode = fundingLineCode;

            return this;
        }

        public ProfileBatchRequestBuilder WithFundingPeriod(string fundingPeriod)
        {
            _fundingPeriod = fundingPeriod;

            return this;
        }

        public ProfileBatchRequestBuilder WithProviderType(string providerType)
        {
            _providerType = providerType;

            return this;
        }

        public ProfileBatchRequestBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public ProfileBatchRequestBuilder WithProfilePatternKey(string profilePatternKey)
        {
            _profilePatternKey = profilePatternKey;

            return this;
        }

        public ProfileBatchRequestBuilder WithProviderSubType(string providerSubType)
        {
            _providerSubType = providerSubType;

            return this;
        }

        public ProfileBatchRequest Build() =>
            new ProfileBatchRequest
            {
                FundingValues = _fundingValues,
                FundingLineCode = _fundingLineCode ?? NewRandomString(),
                FundingPeriodId = _fundingPeriod ?? NewRandomString(),
                ProviderType = _providerType ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                ProfilePatternKey = _profilePatternKey ?? NewRandomString(),
                ProviderSubType = _providerSubType ?? NewRandomString()
            };
    }
}