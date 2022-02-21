using System;

namespace CalculateFunding.Services.Publishing.Undo
{
    public class UndoTaskDetails
    {
        public string SpecificationId { get; set; }

        public string FundingStreamId { get; set; }

        public string FundingPeriodId { get; set; }

        public string CorrelationId { get; set; }
        
        public DateTimeOffset TimeStamp { get; set; }
        
        public decimal Version { get; set; }
    }
}