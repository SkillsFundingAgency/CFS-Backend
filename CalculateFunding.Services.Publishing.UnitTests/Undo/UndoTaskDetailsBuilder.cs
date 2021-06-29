using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Tests.Common.Helpers;
using System;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    public class UndoTaskDetailsBuilder : TestEntityBuilder
    {
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private long? _timeStamp;

        public UndoTaskDetailsBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public UndoTaskDetailsBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public UndoTaskDetailsBuilder WithTimeStamp(long timeStamp)
        {
            _timeStamp = timeStamp;

            return this;
        }
        
        public UndoTaskDetails Build()
        {
            return new UndoTaskDetails
            {
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                TimeStamp = new DateTimeOffset(_timeStamp.GetValueOrDefault(NewRandomTimeStamp()), TimeSpan.Zero)
            };
        }
    }
}