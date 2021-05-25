using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class ProviderConverterBuilder : TestEntityBuilder
    {
        private string _previousProviderIdentifier;
        private string _targetProviderId;

        public ProviderConverterBuilder WithTargetProviderId(string targetProviderId)
        {
            _targetProviderId = targetProviderId;

            return this;
        }

        public ProviderConverterBuilder WithPreviousProviderIdentifier(string previousProviderIdentifier)
        {
            _previousProviderIdentifier = previousProviderIdentifier;

            return this;
        }

        public ProviderConverter Build()
        {
            return new ProviderConverter
            {
                PreviousProviderIdentifier = _previousProviderIdentifier ?? NewRandomString(),
                TargetProviderId = _targetProviderId ?? NewRandomString()
            };
        }
    }
}