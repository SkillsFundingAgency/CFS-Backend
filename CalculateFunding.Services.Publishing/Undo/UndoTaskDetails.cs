namespace CalculateFunding.Services.Publishing.Undo
{
    public class UndoTaskDetails
    {
        public string FundingStreamId { get; set; }

        public string FundingPeriodId { get; set; }
        
        public long TimeStamp { get; set; }
        
        public decimal Version { get; set; }
    }
}