namespace CalculateFunding.Services.Publishing.Models
{
    public class PublishedProviderFundingSummary
    {
        public string ChannelCode { get; set; }
        public string ChannelName { get; set; }
        public string SpecificationId { get; set; }
        public decimal? TotalFunding { get; set; }
        public bool IsIndicative { get; set; }
        public int MajorVersion { get; set; }
        public int MinorVersion { get; set; }
        public string ProviderId { get; set; }
        public string ProviderType { get; set; }
        public string ProviderSubType { get; set; }
        public string Status { get; set; }
    }
}