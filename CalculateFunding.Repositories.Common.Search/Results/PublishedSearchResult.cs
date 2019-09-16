using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Repositories.Common.Search.Results
{    
    public class PublishedSearchResult
    {
        public string Id { get; set; }
        public string ProviderType { get; set; }
        public string LocalAuthority { get; set; }
        public string FundingStatus { get; set; }
        public string ProviderName { get; set; }
        public string UKPRN { get; set; }
        public double FundingValue { get; set; }
        public string SpecificationId { get; set; }
        public string[] FundingStreamIds { get; set; }
        public string FundingPeriodId { get; set; }
    }
}
