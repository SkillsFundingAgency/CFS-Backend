using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.TestEngine.Interfaces;

namespace CalculateFunding.Services.TestRunner.Repositories
{
    [Obsolete("Replace with common nuget API client")]
    public class CalculationsRepository : ICalculationsRepository
    {
        private readonly ICalcsApiClientProxy _apiClient;

        public CalculationsRepository(ICalcsApiClientProxy apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
        }

        public Task<byte[]> GetAssemblyBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"calcs/{specificationId}/assembly";

            return _apiClient.GetAsync<byte[]>(url);
        }
    }
}
