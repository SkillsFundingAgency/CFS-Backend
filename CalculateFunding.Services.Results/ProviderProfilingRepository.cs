using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies.External;
using CalculateFunding.Services.Results.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class ProviderProfilingRepository : IProviderProfilingRepository
    {
        const string profilingUrl = "profiling";

        private readonly IProviderProfilingApiProxy _providerProfilingApiProxy;

        public ProviderProfilingRepository(IProviderProfilingApiProxy providerProfilingApiProxy)
        {
            _providerProfilingApiProxy = providerProfilingApiProxy;
        }

        public Task<ProviderProfilingResponseModel> GetProviderProfilePeriods(ProviderProfilingRequestModel requestModel)
        {
            Guard.ArgumentNotNull(requestModel, nameof(requestModel));

            return _providerProfilingApiProxy.PostAsync<ProviderProfilingResponseModel, ProviderProfilingRequestModel>(profilingUrl, requestModel);
        }
    }
}
