using System.Collections.Generic;

namespace CalculateFunding.Models.External
{
    public class ProviderResultSummary
    {
        public ProviderResultSummary(Period period, Provider provider, FundingStreamResultsSummary[] fundingStreams)
        {
            Period = period;
            Provider = provider;
            FundingStreamsResults = fundingStreams;
        }

        public Period Period { get; set; }

        public Provider Provider { get; set; }

        public IEnumerable<FundingStreamResultsSummary> FundingStreamsResults { get; set; }
    }
}