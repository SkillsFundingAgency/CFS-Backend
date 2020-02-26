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
        private Mock<IOutOfScopePublishedProviderBuilder> _outOfScopeProviderBuilder;
        
        [TestInitialize]
        public void SetUp()
        {
            _outOfScopeProviderBuilder = new Mock<IOutOfScopePublishedProviderBuilder>();

            ClosureVariationStrategy = new ClosureWithSuccessorVariationStrategy(_outOfScopeProviderBuilder.Object);

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
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Successor = NewRandomString());
            
            await WhenTheVariationsAreDetermined();
            
            VariationContext
                .ErrorMessages
                .Should()
                .BeEquivalentTo("Unable to run Closure with Successor variation as could not locate or create a successor provider");


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
                _.GeneratedProvider.TotalFunding += 1M;
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
        public async Task CreatesMissingPublishedProviderAndQueuesTransferAndZeroRemainingProfileAndReAdjustChangesIfPassesPreconditionChecks()
        {
            string successorId = NewRandomString();
            
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Successor = successorId);
            
            PublishedProvider missingProvider = NewPublishedProvider();
            
            AndTheMissingPublishedProviderIsCreated(missingProvider);
            
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
        
        private void AndTheMissingPublishedProviderIsCreated(PublishedProvider missingProvider)
        {
            _outOfScopeProviderBuilder.Setup(_ => _.CreateMissingPublishedProviderForPredecessor(VariationContext, 
                    VariationContext.UpdatedProvider.Successor))
                .ReturnsAsync(missingProvider);
        }
    }
}