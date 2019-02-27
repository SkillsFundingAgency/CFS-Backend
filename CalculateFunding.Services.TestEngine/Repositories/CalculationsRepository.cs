using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.TestEngine.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Repositories
{
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
