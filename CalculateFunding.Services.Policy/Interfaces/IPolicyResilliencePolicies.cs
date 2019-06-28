namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IPolicyResilliencePolicies
    {
        Polly.Policy PolicyRepository { get; set; }

        Polly.Policy CacheProvider { get; set; }
    }
}
