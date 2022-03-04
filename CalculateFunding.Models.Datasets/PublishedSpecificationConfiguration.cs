using System.Collections.Generic;

namespace CalculateFunding.Models.Datasets
{
    public class PublishedSpecificationConfiguration
    {
        public IEnumerable<PublishedSpecificationItem> FundingLines { get; set; }

        public IEnumerable<PublishedSpecificationItem> Calculations { get; set; }

        public string FundingStreamId { get; set; }

        public string FundingPeriodId { get; set; }

        public string SpecificationId { get; set; }

        public bool IncludeCarryForward { get; set; }
    }
}