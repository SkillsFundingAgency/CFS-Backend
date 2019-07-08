using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class PoliciesRepository : IPoliciesRepository
    {
        private readonly IPoliciesApiClientProxy _policiesApiClient;

        public PoliciesRepository(IPoliciesApiClientProxy policiesApiClient)
        {
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));

            _policiesApiClient = policiesApiClient;
        }

        public Task<IEnumerable<FundingStream>> GetFundingStreams()
        {
            string url = $"fundingstreams";

            return _policiesApiClient.GetAsync<IEnumerable<FundingStream>>(url);
        }
    }
}
