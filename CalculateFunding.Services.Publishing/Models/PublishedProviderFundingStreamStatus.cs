namespace CalculateFunding.Services.Publishing.Models
{
    public class PublishedProviderFundingStreamStatus : PublishedProviderFundingCount
    {
        public string FundingStreamId { get; set; }
        public string Status { get; set; }
    }
}
