using CalculateFunding.Services.DeadletterProcessor;
using Polly;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IResultsResiliencePolicies : IJobHelperResiliencePolicies
    {
        Policy CalculationProviderResultsSearchRepository { get; set; }

        Policy ResultsRepository { get; set; }

        Policy ResultsSearchRepository { get; set; }

        Policy SpecificationsApiClient { get; set; }

        Policy ProviderProfilingRepository { get; set; }

        Policy PublishedProviderCalculationResultsRepository { get; set; }

        Policy PublishedProviderResultsRepository { get; set; }

        Policy CalculationsRepository { get; set; }

        Policy ProviderChangesRepository { get; set; }

        Policy ProviderCalculationResultsSearchRepository { get; set; }

        Policy ProviderVersionsSearchRepository { get; set; }

        Policy PoliciesApiClient { get; set; }

        Policy BlobClient { get; set; }
    }
}
