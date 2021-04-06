using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class ApiProviderBuilder : TestEntityBuilder
    {
        private string _providerId;
        private string _status;
        private IEnumerable<string> _predecessors;

        public ApiProviderBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public ApiProviderBuilder WithStatus(string status)
        {
            _status = status;

            return this;
        }

        public ApiProviderBuilder WithPredecessors(params string[] predecessors)
        {
            _predecessors = predecessors;

            return this;
        }
        
        public Provider Build()
        {
            return new Provider
            {
                ProviderId = _providerId ?? NewRandomString(),
                Status = _status ?? NewRandomString(),
                Predecessors = _predecessors
            };
        }
    }
}