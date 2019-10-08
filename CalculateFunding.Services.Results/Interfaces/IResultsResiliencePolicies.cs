using CalculateFunding.Services.Core.Interfaces;
using Polly;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IResultsResiliencePolicies : IJobHelperResiliencePolicies
    {
        Policy CalculationProviderResultsSearchRepository { get; set; }

        Policy ResultsRepository { get; set; }

        Policy ResultsSearchRepository { get; set; }

        Policy SpecificationsRepository { get; set; }

        Policy AllocationNotificationFeedSearchRepository { get; set; }

        Policy ProviderProfilingRepository { get; set; }

        Policy PublishedProviderCalculationResultsRepository { get; set; }

        Policy PublishedProviderResultsRepository { get; set; }

        Policy CalculationsRepository { get; set; }

        Policy JobsApiClient { get; set; }

        Policy ProviderChangesRepository { get; set; }

        Policy ProviderCalculationResultsSearchRepository { get; set; }

        Policy ProviderVersionsSearchRepository { get; set; }

        Policy PoliciesApiClient { get; set; }
    }
}
