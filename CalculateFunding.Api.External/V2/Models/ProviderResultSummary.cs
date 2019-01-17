using System;
using System.Collections.ObjectModel;


namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class ProviderResultSummary
    {
        public ProviderResultSummary()
        {
	        FundingPeriodResults = new Collection<ProviderPeriodResultSummary>();
        }

        public decimal FundingStreamTotalAmount { get; set; }

        public AllocationProviderModel Provider { get; set; }

        public Collection<ProviderPeriodResultSummary> FundingPeriodResults { get; set; }
    }
}