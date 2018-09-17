using System;
using System.Linq;
using System.Xml.Serialization;

namespace CalculateFunding.Api.External.V1.Models
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