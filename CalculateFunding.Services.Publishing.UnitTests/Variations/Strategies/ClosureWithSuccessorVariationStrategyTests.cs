using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class ClosureWithSuccessorVariationStrategyTests : ClosureVariationStrategyTestBase
    {
        private Mock<IProviderService> _providerService;
        
        [TestInitialize]
        public void SetUp()
        {
            _providerService = new Mock<IProviderService>();

            ClosureVariationStrategy = new ClosureWithSuccessorVariationStrategy(_providerService.Object);

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
                .BeEquivalentTo($"Unable to run Closure with Successor variation as could not locate or create a successor provider with id:{successorId}");


            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
        }
        
        [TestMethod]
        public async Task FailsPreconditionCheckSuccessorAlreadyHasThisProviderAsAPredecessor()
        {
            string successorId = NewRandomString();
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Successor = successorId);
            AndTheAllRefreshStateProvider(NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => 
                ppv.WithProviderId(successorId)
                    .WithPredecessors(VariationContext.ProviderId)))));
            
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
        public async Task TracksErrorIfTotalFundingDiffersFromPriorStateWhenGeneratingChanges()
        {
            GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.UpdatedTotalFunding += 1M;
                _.UpdatedProvider.Successor = NewRandomString();
            });

            await WhenTheVariationsAreDetermined();

            VariationContext
                .ErrorMessages
                .Should()
                .BeEquivalentTo("Unable to run Closure with Successor variation as TotalFunding has changed during the refresh funding");

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task QueuesTransferAndZeroRemainingProfileAndReAdjustChangesIfPassesPreconditionChecks()
        {
            string successorId = NewRandomString();
            
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Successor = successorId);
            AndTheAllRefreshStateProvider(NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => 
                ppv.WithProviderId(successorId)))));
            
            await WhenTheVariationsAreDetermined();

            ThenTheVariationChangeWasQueued<TransferRemainingProfilesToSuccessorChange>();
            AndTheVariationChangeWasQueued<ReAdjustSuccessorFundingValuesForProfileValueChange>();
            AndTheVariationChangeWasQueued<ZeroRemainingProfilesChange>();
            AndTheVariationChangeWasQueued<ReAdjustFundingValuesForProfileValuesChange>();
            AndThePredecessorWasAddedToTheSuccessor(VariationContext.ProviderId);
        }

        private void AndThePredecessorWasAddedToTheSuccessor(string successorId)
        {
            VariationContext.SuccessorRefreshState.Predecessors
                .Should()
                .BeEquivalentTo(successorId);
        }
    }
}