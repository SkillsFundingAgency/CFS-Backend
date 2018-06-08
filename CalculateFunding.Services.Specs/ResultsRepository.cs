using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Specs.Interfaces;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs
{
    public class ResultsRepository : IResultsRepository
    {
        const string resultsUrl = "results/publish-provider-results?specificationId=";

        private readonly IApiClientProxy _apiClientProxy;

        public ResultsRepository(IApiClientProxy apiClientProxy)
        {
            _apiClientProxy = apiClientProxy;
        }

        public Task<HttpStatusCode> PublishProviderResults(string specificationId)
        {
            string url = $"{resultsUrl}{specificationId}";

            return _apiClientProxy.PostAsync(url);
        }
    }
}
