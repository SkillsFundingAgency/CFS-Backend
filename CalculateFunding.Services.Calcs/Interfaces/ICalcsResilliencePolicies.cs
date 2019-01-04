using Polly;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalcsResilliencePolicies
    {
        Policy CalculationsRepository { get; set; }

        Policy CalculationsSearchRepository { get; set; }

        Policy CacheProviderPolicy { get; set; }

        Policy CalculationsVersionsRepositoryPolicy { get; set; }

        Policy SpecificationsRepositoryPolicy { get; set; }

        Policy BuildProjectRepositoryPolicy { get; set; }

        Policy MessagePolicy { get; set; }

        Policy JobsApiClient { get; set; }
    }
}
