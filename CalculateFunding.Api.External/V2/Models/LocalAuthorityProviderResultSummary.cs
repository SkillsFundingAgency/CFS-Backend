using System;
using System.Collections.ObjectModel;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class LocalAuthorityProviderResultSummary
    {
        public LocalAuthorityProviderResultSummary()
        {
            FundingPeriods = new Collection<FundingPeriodResultSummary>();
        }
        public AllocationProviderModel Provider { get; set; }

        public decimal AllocationValue { get; set; }

        public Collection<FundingPeriodResultSummary> FundingPeriods { get; set; }
    }
}
