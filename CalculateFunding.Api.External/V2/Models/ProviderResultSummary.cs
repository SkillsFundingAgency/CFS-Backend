using System;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class ProviderResultSummary
    {
        public ProviderResultSummary()
        {
            FundingPeriodResults = new ProviderPeriodResultSummary[0];
        }

        public decimal TotalAmount { get; set; }

        public AllocationProviderModel Provider { get; set; }

        public ProviderPeriodResultSummary[] FundingPeriodResults { get; set; }
    }
}