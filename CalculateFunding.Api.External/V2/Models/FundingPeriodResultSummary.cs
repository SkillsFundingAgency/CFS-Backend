using System;
using System.Collections.ObjectModel;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class FundingPeriodResultSummary
    {
        public FundingPeriodResultSummary()
        {
            Allocations = new Collection<AllocationResultWIthProfilePeriod>();
        }

        public Period Period { get; set; }

        public AllocationFundingStreamModel FundingStream { get; set; }

        public Collection<AllocationResultWIthProfilePeriod> Allocations { get; set; }
    }
}
