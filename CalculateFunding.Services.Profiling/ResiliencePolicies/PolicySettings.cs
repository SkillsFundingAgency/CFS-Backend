namespace CalculateFunding.Services.Profiling.ResiliencePolicies
{
    public class PolicySettings
    {
        public int MaximumSimultaneousNetworkRequests { get; set; } = 500;
    }
}