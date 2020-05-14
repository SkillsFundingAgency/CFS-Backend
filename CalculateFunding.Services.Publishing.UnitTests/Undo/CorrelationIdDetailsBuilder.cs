using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    public class CorrelationIdDetailsBuilder : TestEntityBuilder
    {
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private long? _timeStamp;

        public CorrelationIdDetailsBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public CorrelationIdDetailsBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public CorrelationIdDetailsBuilder WithTimeStamp(long timeStamp)
        {
            _timeStamp = timeStamp;

            return this;
        }
        
        public CorrelationIdDetails Build()
        {
            return new CorrelationIdDetails
            {
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                TimeStamp = _timeStamp.GetValueOrDefault(NewRandomNumberBetween(10000, int.MaxValue))
            };
        }
    }
}