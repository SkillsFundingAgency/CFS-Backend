using System;
using System.Collections.Generic;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class ProviderPeriodResultSummary
    {
        public ProviderPeriodResultSummary()
        {
            FundingStreamResults = new List<FundingStreamResultSummary>();
        }

        public Period Period { get; set; }

        public List<FundingStreamResultSummary> FundingStreamResults { get; set; }
    }
}