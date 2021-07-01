using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Publishing.IntegrationTests.RefreshFunding
{
    public class ProviderSourceDatasetParametersBuilder : TestEntityBuilder
    {
        private string _providerId;
        private string _specificationId;
        private string _dataRelationshipId;

        public ProviderSourceDatasetParametersBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public ProviderSourceDatasetParametersBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public ProviderSourceDatasetParametersBuilder WithDataRelationshipId(
            string dataRelationshipId)
        {
            _dataRelationshipId = dataRelationshipId;

            return this;
        }

        public ProviderSourceDatasetParameters Build() =>
            new ProviderSourceDatasetParameters
            {
                ProviderId = _providerId ?? NewRandomString(),
                SpecificationId = _specificationId ?? NewRandomString(),
                DataRelationshipId = _dataRelationshipId ?? NewRandomString()
            };
    }
}
