using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class PoliciesStepContext : IPoliciesStepContext
    {
        public IPoliciesApiClient Client { get; set; }
        public PoliciesInMemoryRepository Repo { get; set; }
        public string CreateFundingPeriodId { get; set; }
        public string CreateFundingStreamId { get; set; }
        public FundingConfiguration CreateFundingConfiguration { get; set; }
    }
}
