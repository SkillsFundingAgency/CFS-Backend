namespace CalculateFunding.Api.Publishing.IntegrationTests.PublishedProvider
{
    public class PublishedProviderTemplateParameters
    {
        public string Id => $"publishedprovider-{ProviderId}-{FundingPeriodId}-{FundingStream}";

        public string ProviderId { get; set; }
        public string FundingPeriodId { get; set; }
        public string FundingStream { get; set; }
        public string SpecificationId { get; set; }
        public string PublishedProviderId { get; set; }
        public string ProviderType { get; set; }
        public string ProviderSubType { get; set; }
        public string LaCode { get; set; }
        public decimal TotalFunding { get; set; }
        public bool IsIndicative { get; set; }
        public string Status { get; set; }
    }
}
