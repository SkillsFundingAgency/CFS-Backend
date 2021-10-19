using System;

namespace CalculateFunding.Repositories.Common.Search.Results
{    
    public class PublishedSearchResult
    {
        public string Id { get; set; }
        public string ProviderType { get; set; }
        public string ProviderSubType { get; set; }
        public string LocalAuthority { get; set; }
        public string FundingStatus { get; set; }
        public string ProviderName { get; set; }
        public string UKPRN { get; set; }
        public string UPIN { get; set; }
        public string URN { get; set; }
        public double FundingValue { get; set; }
        public string SpecificationId { get; set; }
        public string FundingStreamId { get; set; }
        public string FundingPeriodId { get; set; }
        public string Indicative { get; set; }
        public bool IsIndicative { get; set; }
        public bool HasErrors { get; set; }
        public string[] Errors { get; set; }
        public DateTimeOffset? OpenedDate { get; set; }
        public int? MajorVersion { get; set; }
        public int? MinorVersion { get; set; }
    }
}
