using CalculateFunding.Services.Calcs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Calcs
{
    public class ResiliencePolicies : ICalcsResilliencePolicies
    {
        public Policy CalculationsRepository { get; set; }

        public Policy CalculationsSearchRepository { get; set; }

        public Policy CacheProviderPolicy { get; set; }

        public Policy CalculationsVersionsRepositoryPolicy { get; set; }

        public Policy SpecificationsRepositoryPolicy { get; set; }

        public Policy BuildProjectRepositoryPolicy { get; set; }

        public Policy MessagePolicy { get; set; }
    }
}
