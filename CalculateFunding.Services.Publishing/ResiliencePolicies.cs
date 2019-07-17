using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing
{
    public class ResiliencePolicies : IPublishingResiliencePolicies
    {
        public Policy ResultsRepository { get; set; }
    }
}
