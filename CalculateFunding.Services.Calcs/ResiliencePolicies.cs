using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Interfaces;
using Polly;

namespace CalculateFunding.Services.Calcs
{
    public class ResiliencePolicies : ICalcsResiliencePolicies, IJobHelperResiliencePolicies
    {
        public Policy CalculationsRepository { get; set; }

        public Policy CalculationsSearchRepository { get; set; }

        public Policy CacheProviderPolicy { get; set; }

        public Policy CalculationsVersionsRepositoryPolicy { get; set; }

        public Policy BuildProjectRepositoryPolicy { get; set; }

        public Policy SpecificationsRepositoryPolicy { get; set; }

        public Policy MessagePolicy { get; set; }

        public Policy JobsApiClient { get; set; }

        public Policy SourceFilesRepository { get; set; }

        public Policy DatasetsRepository { get; set; }

        public Policy PoliciesApiClient { get; set; }
        public Policy SpecificationsApiClient { get; set; }
    }
}
