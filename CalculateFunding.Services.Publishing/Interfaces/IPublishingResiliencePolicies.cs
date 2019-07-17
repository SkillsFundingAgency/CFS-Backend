using Polly;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishingResiliencePolicies
    {
        Policy ResultsRepository { get; set; }
        Policy SpecificationsRepositoryPolicy { get; set; }
        Policy JobsApiClient { get; set; }


    }
}
