using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class ClosureWithSuccessorVariationStrategyTests : SuccessorVariationStrategyTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            VariationName = "Closure with Successor";
            
            ClosureVariationStrategy = new ClosureWithSuccessorVariationStrategy(ProviderService.Object);
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
    }
}