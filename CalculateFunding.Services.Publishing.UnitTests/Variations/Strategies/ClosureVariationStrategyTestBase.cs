using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    public abstract class ClosureVariationStrategyTestBase : VariationStrategyTestBase
    {
        protected IVariationStrategy ClosureVariationStrategy;

        [TestMethod]
        public async Task FailsPreconditionCheckIfProviderPreviouslyClosed()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.PriorState.Provider.Status = Variation.Closed);

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
            await ClosureVariationStrategy.DetermineVariations(VariationContext);
        }
    }
}