using System;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Specs.Interfaces;

namespace CalculateFunding.Services.Specs
{
    [Obsolete("Replace with common nuget API client")]
    public class CalculationsRepository : ICalculationsRepository
    {
        const string validateCalculationNameUrl = "calcs/validate-calc-name/{0}/{1}/{2}";

        private readonly ICalcsApiClientProxy _apiClientProxy;

        public CalculationsRepository(ICalcsApiClientProxy apiClientProxy)
        {
            Guard.ArgumentNotNull(apiClientProxy, nameof(apiClientProxy));

            _apiClientProxy = apiClientProxy;
        }

        public async Task<bool> IsCalculationNameValid(string specificationId, string calculationName, string existingCalculationId = null)
        {
            string url = string.Format(validateCalculationNameUrl, specificationId, calculationName, existingCalculationId);

            HttpStatusCode result = await _apiClientProxy.GetAsync(url);

            if (result == HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
