using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Specs.Interfaces;

namespace CalculateFunding.Services.Specs
{
    public class PoliciesRepository : IPoliciesRepository
    {
        const string fundingStreamsApiUrl = "fundingstreams";
        const string fundingPeriodsApiUrl = "fundingperiods";

        private readonly IPoliciesApiClientProxy _apiClientProxy;

        public PoliciesRepository(IPoliciesApiClientProxy apiClientProxy)
        {
            Guard.ArgumentNotNull(apiClientProxy, nameof(apiClientProxy));

            _apiClientProxy = apiClientProxy;
        }

        public async Task<FundingStream> GetFundingStreamById(string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            string url = $"{fundingStreamsApiUrl}/fundingStreamId";

            return await _apiClientProxy.GetAsync<FundingStream>(url);
        }

        public async Task<IEnumerable<FundingStream>> GetFundingStreams(Expression<Func<FundingStream, bool>> query = null)
        {
            string url = fundingStreamsApiUrl;

            IEnumerable<FundingStream> result = await _apiClientProxy.GetAsync<IEnumerable<FundingStream>>(url);
            return result.AsQueryable().Where(query);
        }

        public Task<HttpStatusCode> SaveFundingStream(FundingStream fundingStream)
        {
            throw new NotImplementedException();
        }

        public async Task<Period> GetPeriodById(string periodId)
        {
            Guard.IsNullOrWhiteSpace(periodId, nameof(periodId));

            string url = $"{fundingPeriodsApiUrl}/{periodId}";

            return await _apiClientProxy.GetAsync<Period>(url);
        }

        public async Task<IEnumerable<Period>> GetPeriods()
        {
            string url = fundingPeriodsApiUrl;

            return await _apiClientProxy.GetAsync<IEnumerable<Period>>(url);
        }

        public Task SavePeriods(IEnumerable<Period> periods)
        {
            throw new NotImplementedException();
        }
    }
}
