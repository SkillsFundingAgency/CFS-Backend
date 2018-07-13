using System.Collections.Generic;

namespace CalculateFunding.Models.External
{
    public class ProviderFundingStreamResult
    {
        public ProviderFundingStreamResult()
        {
        }

        public ProviderFundingStreamResult(Period period, Provider provider, FundingStream fundingStream, IEnumerable<PolicyResult> policyResults)
        {
            Period = period;
            Provider = provider;
            FundingStream = fundingStream;
            PolicyResults = policyResults;
        }

        public Period Period { get; set; }

        public Provider Provider { get; set; }

        public FundingStream FundingStream { get; set; }

        public IEnumerable<PolicyResult> PolicyResults { get; set; }
    }
}