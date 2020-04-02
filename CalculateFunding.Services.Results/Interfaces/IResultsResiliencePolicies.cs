using CalculateFunding.Services.DeadletterProcessor;
using Polly;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IResultsResiliencePolicies : IJobHelperResiliencePolicies
    {
        AsyncPolicy CalculationProviderResultsSearchRepository { get; set; }

        AsyncPolicy ResultsRepository { get; set; }

        AsyncPolicy ResultsSearchRepository { get; set; }

        AsyncPolicy SpecificationsApiClient { get; set; }

        AsyncPolicy ProviderProfilingRepository { get; set; }

        AsyncPolicy PublishedProviderCalculationResultsRepository { get; set; }

        AsyncPolicy PublishedProviderResultsRepository { get; set; }

        AsyncPolicy CalculationsRepository { get; set; }

        AsyncPolicy ProviderChangesRepository { get; set; }

        AsyncPolicy ProviderCalculationResultsSearchRepository { get; set; }

        AsyncPolicy ProviderVersionsSearchRepository { get; set; }

        AsyncPolicy PoliciesApiClient { get; set; }

        AsyncPolicy BlobClient { get; set; }
    }
}
