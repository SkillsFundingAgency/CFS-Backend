using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    public class ProviderDatasetRowParametersBuilder : TestEntityBuilder
    {
        private string _ukprn;
        private string _name;
        private string _providerType;
        private string _providerSubType;
        private string[] _predecessors;
        private string[] _successors;
        private string _status;

        public ProviderDatasetRowParametersBuilder WithUkprn(string ukprn)
        {
            _ukprn = ukprn;

            return this;
        }

        public ProviderDatasetRowParametersBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public ProviderDatasetRowParametersBuilder WithStatus(string status)
        {
            _status = status;

            return this;
        }

        public ProviderDatasetRowParametersBuilder WithType(string providerType)
        {
            _providerType = providerType;

            return this;
        }

        public ProviderDatasetRowParametersBuilder WithSubType(string providerSubType)
        {
            _providerSubType = providerSubType;

            return this;
        }

        public ProviderDatasetRowParametersBuilder WithPredecessors(params string[] predecessors)
        {
            _predecessors = predecessors;

            return this;
        }

        public ProviderDatasetRowParametersBuilder WithSuccessors(params string[] successors)
        {
            _successors = successors;

            return this;
        }

        public ProviderDatasetRowParameters Build() =>
            new ProviderDatasetRowParameters
            {
                Ukprn = _ukprn ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                Status = _status ?? NewRandomString(),
                Predecessors = _predecessors,
                Successors = _successors,
                ProviderType = _providerType ?? NewRandomString(),
                ProviderSubType = _providerSubType ?? NewRandomString(),
            };
    }
}