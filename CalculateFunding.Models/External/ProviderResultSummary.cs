using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.External
{
    [Serializable]
    public class ProviderResultSummary
    {
        public ProviderResultSummary()
        {
        }

        public ProviderResultSummary(Period period, Provider provider, List<FundingStreamResultSummary> fundingStreams)
        {
            Period = period;
            Provider = provider;
            FundingStreamResults = fundingStreams;
        }

        public Period Period { get; set; }

        public Provider Provider { get; set; }

        public List<FundingStreamResultSummary> FundingStreamResults { get; set; }
    }
}