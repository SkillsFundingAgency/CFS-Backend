using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class ProviderSourceDatasetHistoryBuilder : TestEntityBuilder
    {
        private string _providerId;

        public ProviderSourceDatasetHistoryBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }
        
        public ProviderSourceDatasetHistory Build()
        {
            return new ProviderSourceDatasetHistory
            {
                ProviderId = _providerId 
            };
        }
    }
}