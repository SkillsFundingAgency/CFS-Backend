namespace CalculateFunding.Models.External
{
    public class ProviderPolicyResult
    {
        public ProviderPolicyResult()
        {
        }

        public ProviderPolicyResult(Period period, Provider provider, PolicyResult policyResult)
        {
            Period = period;
            Provider = provider;
            PolicyResult = policyResult;
        }

        public Period Period { get; set; }

        public Provider Provider { get; set; }

        public PolicyResult PolicyResult { get; set; }
    }
}