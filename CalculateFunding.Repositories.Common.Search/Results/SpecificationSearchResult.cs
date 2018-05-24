using System;
using System.Text;

namespace CalculateFunding.Repositories.Common.Search.Results
{
    public class SpecificationSearchResult
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string FundingPeriodName { get; set; }

        public string[] FundingStreamNames { get; set; }

        public string Status { get; set; }

        public string Description { get; set; }

        public DateTimeOffset? LastUpdatedDate { get; set; }
    }
}
