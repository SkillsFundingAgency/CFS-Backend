using System.Linq;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Publishing.IntegrationTests.RefreshFunding
{
    public class ProviderVersionTemplateParametersBuilder : TestEntityBuilder
    {
        private string _id;
        private Provider[] _providers;

        public ProviderVersionTemplateParametersBuilder()
        {
            _id = NewRandomString();
        }

        public ProviderVersionTemplateParametersBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public ProviderVersionTemplateParametersBuilder WithProviders(params ProviderDatasetRowParameters[] providers)
        {
            _providers = providers.Select(_ => new Provider
            {
                ProviderId = _.Ukprn,
                ProviderVersionId = _id,
                ProviderVersionIdProviderId = $"{_id}_{_.Ukprn}",
                UKPRN = _.Ukprn,
                Status = _.Status,
                ProviderType = _.ProviderType,
                ProviderSubType = _.ProviderSubType,
                Predecessors = _.Predecessors,
                Successors = _.Successors
            }).ToArray();

            return this;
        }

        public ProviderVersionTemplateParameters Build() =>
            new ProviderVersionTemplateParameters
            {
                Id = _id,
                Providers = _providers ?? new Provider[0]
            };
    }
}