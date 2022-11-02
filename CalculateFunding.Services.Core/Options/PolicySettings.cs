namespace CalculateFunding.Services.Core.Options
{
    public class PolicySettings
    {
        public int MaximumSimultaneousNetworkRequests { get; set; } = 500;
        public int MaximumQueuingActions { get; set; } = 3000;
    }
}
