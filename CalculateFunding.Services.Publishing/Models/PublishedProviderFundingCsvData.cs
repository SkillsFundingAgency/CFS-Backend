namespace CalculateFunding.Services.Publishing.Models
{
    public class PublishedProviderFundingCsvData
    {
        public string SpecificationId { get; set; }
        public string FundingStreamId { get; set; }
        public string FundingPeriodId { get; set; }
        public string ProviderName { get; set; }
        public string Ukprn { get; set; }
        public string Urn { get; set; }
        public string Upin { get; set; }
        public decimal? TotalFunding { get; set; }
        public string Status { get; set; }
        public bool? IsIndicative { get; set; }
        public int? MinorVersion { get; set; }
        public int? MajorVersion { get; set; }
        public string[] VariationReasons { get; set; }
        public int? LastReleasedMajorVersion { get; set; }
        public int? LastReleasedMinorVersion { get; set; }
        public decimal? LastReleasedTotalFunding { get; set; }
        public string ProviderId { get; set; }
    }
}
