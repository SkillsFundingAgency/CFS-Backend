using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    public abstract class VariationStrategyTestBase : ProviderVariationContextTestBase
    {
        protected void GivenTheOtherwiseValidVariationContext(Action<ProviderVariationContext> changes)
        {
            VariationContext.AllPublishedProviderSnapShots = AsDictionary(new  PublishedProviderSnapShots(VariationContext.PublishedProvider));
            VariationContext.AllPublishedProvidersRefreshStates = AsDictionary(VariationContext.PublishedProvider);
            
            changes(VariationContext);
        }

        protected void AndTheAllRefreshStateProvider(PublishedProvider publishedProvider)
        {
            VariationContext.AllPublishedProvidersRefreshStates[publishedProvider.Current.ProviderId] = publishedProvider;
        }

        private IDictionary<string, PublishedProvider> AsDictionary(params PublishedProvider[] publishedProviders)
        {
            return publishedProviders.ToDictionary(_ => _.Current.ProviderId);
        }

        private IDictionary<string, PublishedProviderSnapShots> AsDictionary(params PublishedProviderSnapShots[] publishedProviders)
        {
            return publishedProviders.ToDictionary(_ => _.LatestSnapshot.Current.ProviderId);
        }
    }
}