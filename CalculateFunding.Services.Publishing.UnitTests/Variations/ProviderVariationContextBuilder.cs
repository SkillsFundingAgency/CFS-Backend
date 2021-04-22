using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations
{
    public class ProviderVariationContextBuilder : TestEntityBuilder
    {
        private Provider _currentState;
        private PublishedProvider _publishedProvider;
        private decimal? _updatedTotalFunding;
        private IEnumerable<string> _errors;
        private IPoliciesService _policiesService;

        public ProviderVariationContextBuilder WithUpdatedTotalFunding(decimal? updatedTotalFunding)
        {
            _updatedTotalFunding = updatedTotalFunding;

            return this;
        }

        public ProviderVariationContextBuilder WithErrors(params string[] errors)
        {
            _errors = errors;

            return this;
        }

        public ProviderVariationContextBuilder WithPublishedProvider(PublishedProvider publishedProvider)
        {
            _publishedProvider = publishedProvider;

            return this;
        }

        public ProviderVariationContextBuilder WithCurrentState(Provider provider)
        {
            _currentState = provider;

            return this;
        }

        public ProviderVariationContextBuilder WithPoliciesService(IPoliciesService policiesService)
        {
            _policiesService = policiesService;

            return this;
        }

        public ProviderVariationContext Build()
        {
            ProviderVariationContext providerVariationContext = new ProviderVariationContext(_policiesService)
            {
                UpdatedProvider = _currentState,
                PublishedProvider = _publishedProvider,
                UpdatedTotalFunding = _updatedTotalFunding
            };

            if (_errors?.Any() == true)
            {
                providerVariationContext.ErrorMessages.AddRange(_errors);
            }

            return providerVariationContext;
        }
    }
}