using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class BatchProfilingRequestModelBuilder : TestEntityBuilder
    {
        private string _providerType;
        private string _providerSubType;
        private decimal[] _fundingValues;
        private string _fundingLineCode;
        private string _fundingPeriodId;
        private string _fundingStreamId;
        private string _profilePatternKey;

        public BatchProfilingRequestModelBuilder WithProviderType(string providerType)
        {
            _providerType = providerType;

            return this;
        }

        public BatchProfilingRequestModelBuilder WithProviderSubType(string providerSubType)
        {
            _providerSubType = providerSubType;

            return this;
        }

        public BatchProfilingRequestModelBuilder WithFundingValues(params decimal[] fundingValues)
        {
            _fundingValues = fundingValues;

            return this;
        }

        public BatchProfilingRequestModelBuilder WithFundingLineCode(string fundingLineCode)
        {
            _fundingLineCode = fundingLineCode;

            return this;
        }

        public BatchProfilingRequestModelBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public BatchProfilingRequestModelBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public BatchProfilingRequestModelBuilder WithProfilePatternKey(string profilePatternKey)
        {
            _profilePatternKey = profilePatternKey;

            return this;
        }

        public BatchProfilingRequestModel Build() =>
            new BatchProfilingRequestModel
            {
                ProviderType = _providerType,
                ProviderSubType = _providerSubType,
                FundingValues = _fundingValues,
                FundingLineCode = _fundingLineCode,
                FundingPeriodId = _fundingPeriodId,
                FundingStreamId = _fundingStreamId,
                ProfilePatternKey = _profilePatternKey
            };
    }
}