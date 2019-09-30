using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IPoliciesStepContext
    {
        IPoliciesApiClient Client { get; set; }

        PoliciesInMemoryRepository Repo { get; set; }
        string CreateFundingPeriodId { get; set; }
        string CreateFundingStreamId { get; set; }
        FundingConfiguration CreateFundingConfiguration { get; set; }
    }
}
