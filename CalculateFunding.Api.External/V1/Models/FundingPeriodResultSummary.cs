using System;

namespace CalculateFunding.Api.External.V1.Models
{
    [Serializable]
    public class FundingPeriodResultSummary
    {
        public FundingPeriodResultSummary()
        {
            Allocations = new AllocationResultWIthProfilePeriod[0];
        }

        public Period FundingPeriod { get; set; }

        public AllocationResultWIthProfilePeriod[] Allocations { get; set; }
    }
}
