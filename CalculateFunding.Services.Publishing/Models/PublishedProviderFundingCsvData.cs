using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
