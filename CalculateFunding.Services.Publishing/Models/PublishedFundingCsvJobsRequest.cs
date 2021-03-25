using CalculateFunding.Common.Models;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.Models
{
    public class PublishedFundingCsvJobsRequest
    {
        public string SpecificationId { get; set; }
        public string CorrelationId { get; set; }
        public Reference User { get; set; }
        public IEnumerable<string> FundingLineCodes { get; set; }
        public IEnumerable<string> FundingStreamIds { get; set; }
        public string FundingPeriodId { get; set; }
    }
}
