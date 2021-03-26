using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class EligibleConverterBuilder : TestEntityBuilder
    {
        private string _previousProviderIdentifier;
        private string _providerId;

        public EligibleConverterBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public EligibleConverterBuilder WithPreviousProviderIdentifier(string previousProviderIdentifier)
        {
            _previousProviderIdentifier = previousProviderIdentifier;

            return this;
        }

        public EligibleConverter Build()
        {
            return new EligibleConverter
            {
                PreviousProviderIdentifier = _previousProviderIdentifier ?? NewRandomString(),
                ProviderId = _providerId ?? NewRandomString(),
            };
        }
    }
}