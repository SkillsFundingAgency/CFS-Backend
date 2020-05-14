using System;

namespace CalculateFunding.Services.Publishing.Undo
{
    public class CorrelationIdDetails
    {
        public string FundingStreamId { get; set; }

        public string FundingPeriodId { get; set; }
        
        public long TimeStamp { get; set; }
    }
}