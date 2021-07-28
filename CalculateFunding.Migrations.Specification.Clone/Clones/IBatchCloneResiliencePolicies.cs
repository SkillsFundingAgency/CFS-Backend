using Polly;

namespace CalculateFunding.Migrations.Specification.Clone.Clones
{
    public interface IBatchCloneResiliencePolicies
    {
        AsyncPolicy JobsApiClient { get; set; }

        AsyncPolicy PoliciesApiClient { get; set; }

        AsyncPolicy SpecificationsApiClient { get; set; }

        AsyncPolicy CalcsApiClient { get; set; }

        AsyncPolicy DatasetsApiClient { get; set; }
    }
}
