using CalculateFunding.Models.Providers;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Providers.UnitTests
{
    public class ProviderVersionBuilder : TestEntityBuilder
    {
        private string _name;
        private string _type;
        private int? _version;
        private string _fundingStream;

        public ProviderVersionBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public ProviderVersionBuilder WithType(string type)
        {
            _type = type;

            return this;
        }

        public ProviderVersionBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }

        public ProviderVersionBuilder WithFundingStream(string fundingStream)
        {
            _fundingStream = fundingStream;

            return this;
        }
        
        public ProviderVersion Build()
        {
            return new ProviderVersion
            {
                Name = _name ?? NewRandomString(),
                Version = _version.GetValueOrDefault(NewRandomNumberBetween(1, 100)),
                FundingStream = _fundingStream ?? NewRandomString(),
                ProviderVersionTypeString = _type ?? NewRandomString(),
            };
        }
    }
}