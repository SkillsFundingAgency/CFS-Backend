using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.External
{
    [Serializable]
    public class ProviderFundingStreamResult
    {
        public ProviderFundingStreamResult()
        {
        }

        public ProviderFundingStreamResult(Period period, Provider provider, FundingStream fundingStream, List<PolicyResult> policyResults)
        {
            Period = period;
            Provider = provider;
            FundingStream = fundingStream;
            PolicyResults = policyResults;
        }

        public Period Period { get; set; }

        public Provider Provider { get; set; }

        public FundingStream FundingStream { get; set; }

        public List<PolicyResult> PolicyResults { get; set; }
    }
}