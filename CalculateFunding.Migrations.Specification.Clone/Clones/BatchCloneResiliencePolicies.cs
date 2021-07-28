using Polly;

namespace CalculateFunding.Migrations.Specification.Clone.Clones
{
    public class BatchCloneResiliencePolicies : IBatchCloneResiliencePolicies
    {
        public AsyncPolicy JobsApiClient { get; set; }
        public AsyncPolicy PoliciesApiClient { get; set; }
        public AsyncPolicy SpecificationsApiClient { get; set; }
        public AsyncPolicy CalcsApiClient { get; set; }
        public AsyncPolicy DatasetsApiClient { get; set; }
    }
}
