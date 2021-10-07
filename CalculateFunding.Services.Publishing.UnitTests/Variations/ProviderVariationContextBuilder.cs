using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations;
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
        private IDictionary<string, PublishedProviderSnapShots> _allPublishedProviderSnapshots;
        private ICollection<VariationReason> _variationReasons;

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

        public ProviderVariationContextBuilder WithAllPublishedProviderSnapShots(IDictionary<string, PublishedProviderSnapShots> allPublishedProviderSnapshots)
        {
            _allPublishedProviderSnapshots = allPublishedProviderSnapshots;

            return this;
        }

        public ProviderVariationContextBuilder WithVariationReasons(params VariationReason[] variationReasons)
        {
            _variationReasons = variationReasons;

            return this;
        }

        public ProviderVariationContext Build()
        {
            ProviderVariationContext providerVariationContext = new ProviderVariationContext(_policiesService)
            {
                UpdatedProvider = _currentState,
                PublishedProvider = _publishedProvider,
                UpdatedTotalFunding = _updatedTotalFunding,
                AllPublishedProviderSnapShots= _allPublishedProviderSnapshots,
                VariationReasons = _variationReasons ?? new List<VariationReason>()
            };

            if (_errors?.Any() == true)
            {
                providerVariationContext.ErrorMessages.AddRange(_errors);
            }

            return providerVariationContext;
        }
    }
}