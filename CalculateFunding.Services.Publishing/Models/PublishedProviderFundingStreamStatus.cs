namespace CalculateFunding.Services.Publishing.Models
{
    public class PublishedProviderFundingStreamStatus
    {
        public int Count { get; set; }
        public string FundingStreamId { get; set; }
        public string Status { get; set; }
        public decimal? TotalFunding { get; set; }
    }
}
