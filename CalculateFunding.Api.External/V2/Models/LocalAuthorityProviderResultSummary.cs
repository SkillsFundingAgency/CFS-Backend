using System;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class LocalAuthorityProviderResultSummary
    {
        public LocalAuthorityProviderResultSummary()
        {
            FundingPeriods = new FundingPeriodResultSummary[0];
        }
        public AllocationProviderModel Provider { get; set; }

        public decimal AllocationValue { get; set; }

        public FundingPeriodResultSummary[] FundingPeriods { get; set; }
    }
}
