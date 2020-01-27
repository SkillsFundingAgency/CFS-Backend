using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class ClosureVariationStrategyTests : ProviderVariationContextTestBase
    {
        private ClosureVariationStrategy _closureVariationStrategy;

        [TestInitialize]
        public void SetUp()
        {
            _closureVariationStrategy = new ClosureVariationStrategy();
        }

        [TestMethod]
        public async Task TracksErrorIfTotalFundingDiffersFromPriorStateWhenGeneratingChanges()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.GeneratedProvider.TotalFunding += 1M);

            await WhenTheVariationsAreDetermined();

            VariationContext
                .ErrorMessages
                .Should()
                .BeEquivalentTo("Unable to run Closure variation as TotalFunding has changed during the refresh funding");

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfProviderPreviouslyClosed()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.ReleasedState.Provider.Status = ClosureVariationStrategy.Closed);

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

        [TestMethod]
        public async Task FailsPreconditionCheckIfUpdatedProviderHasASuccessor()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Successor = NewRandomString());

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
        public async Task QueuesZeroRemainingProfileAndCloseProviderChangesIfPassesPreconditionChecks()
        {
            await WhenTheVariationsAreDetermined();

            ThenTheVariationChangeWasQueued<ZeroRemainingProfilesChange>();
            AndTheVariationChangeWasQueued<ReAdjustFundingValuesForProfileValuesChange>();
        }

        private async Task WhenTheVariationsAreDetermined()
        {
            await _closureVariationStrategy.DetermineVariations(VariationContext);
        }

        private void GivenTheOtherwiseValidVariationContext(Action<ProviderVariationContext> invalidChanges)
        {
            invalidChanges(VariationContext);
        }
    }
}