using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Results.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Repositories
{
    public class PoliciesRepository : IPoliciesRepository
    {
        private const string fundingStreamsUrl = "fundingstreams";
        private const string fundingPeriodUrl = "fundingperiods/";

        private readonly IPoliciesApiClientProxy _policiesApiClient;

        public PoliciesRepository(IPoliciesApiClientProxy policiesApiClient)
        {
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));

            _policiesApiClient = policiesApiClient;
        }

        public async Task<IEnumerable<FundingStream>> GetFundingStreams()
        {
            return await _policiesApiClient.GetAsync<IEnumerable<FundingStream>>(fundingStreamsUrl);
        }

        public async Task<Period> GetFundingPeriodById(string fundingPeriodId)
        {
            Guard.ArgumentNotNull(fundingPeriodId, nameof(fundingPeriodId));

            string url = $"{fundingPeriodUrl}{fundingPeriodId}";

            return await _policiesApiClient.GetAsync<Period>(url);
        }
    }
}
