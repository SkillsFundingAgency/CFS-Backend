using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;
using Provider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations
{
    public class ProviderVariationContextBuilder : TestEntityBuilder
    {
        private Provider _currentState;
        private PublishedProviderVersion _priorState;
        private ProviderVariationResult _result;
        private IEnumerable<string> _errors;

        public ProviderVariationContextBuilder WithErrors(params string[] errors)
        {
            _errors = errors;

            return this;
        }

        public ProviderVariationContextBuilder WithResult(ProviderVariationResult result)
        {
            _result = result;

            return this;
        }

        public ProviderVariationContextBuilder WithCurrentState(Provider provider)
        {
            _currentState = provider;

            return this;
        }

        public ProviderVariationContextBuilder WithPriorState(PublishedProviderVersion publishedProviderVersion)
        {
            _priorState = publishedProviderVersion;

            return this;
        }
        
        public ProviderVariationContext Build()
        {
            ProviderVariationContext providerVariationContext = new ProviderVariationContext
            {
                UpdatedProvider = _currentState,
                PriorState = _priorState,
                Result = _result ?? new ProviderVariationResult(),
            };

            if (_errors?.Any() == true)
            {
                providerVariationContext.ErrorMessages.AddRange(_errors);
            }
            
            return providerVariationContext;
        }
    }
}