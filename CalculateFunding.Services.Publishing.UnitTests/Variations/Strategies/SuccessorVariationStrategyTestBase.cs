using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    public abstract class SuccessorVariationStrategyTestBase : ClosureVariationStrategyTestBase
    {
        protected Mock<IProviderService> ProviderService;
        protected string VariationName;

        [TestInitialize]
        public void SuccessorVariationStrategyTestBaseSetUp()
        {
            ProviderService = new Mock<IProviderService>();

            VariationContext.SuccessorRefreshState = VariationContext.RefreshState.DeepCopy();

            VariationContext.UpdatedProvider.Status = Variation.Closed;
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfUpdatedProviderHasNoASuccessor()
        {
            GivenTheOtherwiseValidVariationContext();

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
        public async Task FailsPreconditionCheckSuccessorNotLocatedInVariationContextOrCreatedFromCoreProviderData()
        {
            string successorId = NewRandomString();
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Successor = successorId);
            
            await WhenTheVariationsAreDetermined();
            
            VariationContext
                .ErrorMessages
                .Should()
                .BeEquivalentTo($"Unable to run {VariationName} variation as could not locate or create a successor provider with id:{successorId}");

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
        }

        protected void AndThePredecessorWasAddedToTheSuccessor(string successorId)
        {
            VariationContext.SuccessorRefreshState.Predecessors
                .Should()
                .BeEquivalentTo(successorId);
        }
    }
}