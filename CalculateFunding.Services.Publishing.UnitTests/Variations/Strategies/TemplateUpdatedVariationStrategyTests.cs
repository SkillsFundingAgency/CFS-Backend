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
    public class TemplateUpdatedVariationStrategyTests : VariationStrategyTestBase
    {
        private TemplateUpdatedVariationStrategy _templateUpdatedVariationStrategy;

        [TestInitialize]
        public void SetUp()
        {
            _templateUpdatedVariationStrategy = new TemplateUpdatedVariationStrategy();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfNoPriorStateYet()
        {
            GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.AllPublishedProviderSnapShots = new Dictionary<string, PublishedProviderSnapShots>();
                _.AllPublishedProvidersRefreshStates = new Dictionary<string, PublishedProvider>();
            });

            await WhenTheVariationsAreProcessed();

            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfThereIsNoPreviouslyReleasedVersion()
        {
            GivenTheOtherwiseValidVariationContext();
            AndThereIsNoPreviouslyReleasedVersion();

            await WhenTheVariationsAreProcessed();

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task SetTheVariationReasonIfTemplateVersionIsUpated()
        {
            GivenTheOtherwiseValidVariationContext(_ => 
            {
                _.PriorState.TemplateVersion = "1.0";
                _.RefreshState.TemplateVersion = "2.0";
            });

            await WhenTheVariationsAreProcessed();

            VariationContext
                .VariationReasons
                .Should()
                .BeEquivalentTo(new[] { VariationReason.TemplateUpdated});

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

        private async Task WhenTheVariationsAreProcessed()
        {
            await _templateUpdatedVariationStrategy.Process(VariationContext, null);
        }

        private void AndThereIsNoPreviouslyReleasedVersion()
        {
            VariationContext.PublishedProvider.Released = null;
        }
    }
}
