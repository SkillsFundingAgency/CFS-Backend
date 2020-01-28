using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    public abstract class ClosureVariationStrategyTestBase : ProviderVariationContextTestBase
    {
        protected IVariationStrategy _closureVariationStrategy;

        [TestMethod]
        public async Task FailsPreconditionCheckIfProviderPreviouslyClosed()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.ReleasedState.Provider.Status = ClosureVariation.Closed);

            await WhenTheVariationsAreDetermined();

            VariationContext
                .ErrorMessages
                .Should()
                .BeEmpty();

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfUpdatedProviderNotClosed()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = NewRandomString());

            await WhenTheVariationsAreDetermined();

            VariationContext
                .ErrorMessages
                .Should()
                .BeEmpty();

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
        }

        protected async Task WhenTheVariationsAreDetermined()
        {
            await _closureVariationStrategy.DetermineVariations(VariationContext);
        }

        protected void GivenTheOtherwiseValidVariationContext(Action<ProviderVariationContext> invalidChanges)
        {
            VariationContext.AllPublishedProviderSnapShots = AsDictionary(VariationContext.PublishedProvider.DeepCopy());
            VariationContext.AllPublishedProvidersRefreshStates = AsDictionary(VariationContext.PublishedProvider);
            
            invalidChanges(VariationContext);
        }

        protected void AndTheAllRefreshStateProvider(PublishedProvider publishedProvider)
        {
            VariationContext.AllPublishedProvidersRefreshStates[publishedProvider.Current.ProviderId] = publishedProvider;
        }

        private IDictionary<string, PublishedProvider> AsDictionary(params PublishedProvider[] publishedProviders)
        {
            return publishedProviders.ToDictionary(_ => _.Current.ProviderId);
        }
    }
}