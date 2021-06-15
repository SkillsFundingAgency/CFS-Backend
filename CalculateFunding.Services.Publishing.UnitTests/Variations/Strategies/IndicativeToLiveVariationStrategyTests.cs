using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class IndicativeToLiveVariationStrategyTests : VariationStrategyTestBase
    {
        private IndicativeToLiveVariationStrategy _indicativeToLiveVariationStrategy;

        [TestInitialize]
        public void SetUp()
        {
            _indicativeToLiveVariationStrategy = new IndicativeToLiveVariationStrategy();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfNoPriorStateYet()
        {
            GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.AllPublishedProviderSnapShots = new Dictionary<string, PublishedProviderSnapShots>();
                _.AllPublishedProvidersRefreshStates = new Dictionary<string, PublishedProvider>();
            });

            await WhenTheVariationsAreDetermined();

            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task SetTheVariationReasonIfFundingSchemaVersionIsUpated()
        {
            GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.PriorState.IsIndicative = true;
                _.RefreshState.IsIndicative = false;
            });

            await WhenTheVariationsAreDetermined();

            VariationContext
                .VariationReasons
                .Should()
                .BeEquivalentTo(new[] { VariationReason.IndicativeToLive });

            VariationContext
               .QueuedChanges
               .Should()
               .NotBeEmpty();

            VariationContext
              .QueuedChanges
              .First()
              .Should()
              .BeOfType<MetaDataVariationsChange>();
        }

        private async Task WhenTheVariationsAreDetermined()
        {
            await _indicativeToLiveVariationStrategy.DetermineVariations(VariationContext, null);
        }
    }
}
