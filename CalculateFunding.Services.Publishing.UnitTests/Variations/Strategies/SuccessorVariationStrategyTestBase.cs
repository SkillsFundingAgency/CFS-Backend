using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
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

            VariationContext.Successor = new PublishedProvider { Current = VariationContext.RefreshState.DeepCopy() };

            VariationContext.UpdatedProvider.Status = VariationStrategy.Closed;
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfUpdatedProviderHasNoASuccessor()
        {
            GivenTheOtherwiseValidVariationContext();

            await WhenTheVariationsAreProcessed();

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
            
            await WhenTheVariationsAreProcessed();

            IEnumerable<string> errorFieldValues = GetFieldValues(VariationContext.ErrorMessages.First());

            errorFieldValues
                .First()
                .Should()
                .Be(VariationContext.ProviderId);

            errorFieldValues
                .Skip(1)
                .First()
                .Should()
                .Be($"Unable to apply strategy '{VariationName}'");


            errorFieldValues
                .Skip(2)
                .First()
                .Should()
                .Be($"Could not locate or create a successor provider with id:{successorId}");

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
        }

        protected void AndThePredecessorWasAddedToTheSuccessor(string successorId)
        {
            VariationContext.Successor.Current.Predecessors
                .Should()
                .BeEquivalentTo(successorId);
        }
        private IEnumerable<string> GetFieldValues(string errorRow)
        {
            return errorRow
                .Split("\",\"")
                .Select(_ =>
                    _.Replace("\"", "")
                );
        }
    }
}