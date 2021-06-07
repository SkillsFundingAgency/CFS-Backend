using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Models
{
    public class PublishedProviderFunding
    {
        public string SpecificationId { get; set; }
        public string PublishedProviderId { get; set; }
        public ProviderTypeSubType ProviderTypeSubType { get; set; }
        public string LaCode { get; set; }
        public string FundingStreamId { get; set; }
        public decimal? TotalFunding { get; set; }
        public bool IsIndicative { get; set; }
    }
}