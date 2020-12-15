using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class ProviderSourceDatasetBuilder : TestEntityBuilder
    {
        private string _dataDefinitionId;
        private string _providerId;
        private string _specificationId;
        private ProviderSourceDatasetVersion _current;

        public ProviderSourceDatasetBuilder WithCurrent(ProviderSourceDatasetVersion current)
        {
            _current = current;

            return this;
        }
        
        public ProviderSourceDatasetBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }
        
        public ProviderSourceDatasetBuilder WithDataDefinitionId(string dataDefinitionId)
        {
            _dataDefinitionId = dataDefinitionId;

            return this;
        }

        public ProviderSourceDatasetBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }
        
        public ProviderSourceDataset Build()
        {
            return new ProviderSourceDataset()
            {
                ProviderId = _providerId ?? NewRandomString(),
                SpecificationId = _specificationId ?? NewRandomString(),
                DataDefinitionId = _dataDefinitionId ?? NewRandomString(), 
                DataDefinition = new ReferenceBuilder()
                    .WithId(_dataDefinitionId)
                    .Build(),
                Current = _current,
            }; 
        }
    }
}