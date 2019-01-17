using System;
using System.Collections.ObjectModel;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class ProviderPeriodResultSummary
    {
        public ProviderPeriodResultSummary()
        {
            FundingStreamResults = new Collection<FundingStreamResultSummary>();
        }

        public Period Period { get; set; }

        public Collection<FundingStreamResultSummary> FundingStreamResults { get; set; }
    }
}