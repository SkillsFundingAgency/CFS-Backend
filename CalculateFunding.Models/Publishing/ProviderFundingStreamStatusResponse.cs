namespace CalculateFunding.Models.Publishing
{
    public class ProviderFundingStreamStatusResponse
    {
        public string FundingStreamId { get; set; }
        public int ProviderDraftCount { get; set; }
        public int ProviderApprovedCount { get; set; }
        public int ProviderUpdatedCount { get; set; }
        public int ProviderReleasedCount { get; set; }
    }
}
