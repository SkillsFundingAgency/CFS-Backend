using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class PoliciesStepContext : IPoliciesStepContext
    {
        public PoliciesStepContext(IPoliciesApiClient policiesApiClient)
        {
            Client = policiesApiClient;
        }

        public IPoliciesApiClient Client { get; }

        public PoliciesInMemoryRepository Repo => (PoliciesInMemoryRepository) Client;

        public string CreateFundingPeriodId { get; set; }

        public string CreateFundingStreamId { get; set; }

        public FundingConfiguration CreateFundingConfiguration { get; set; }
    }
}
