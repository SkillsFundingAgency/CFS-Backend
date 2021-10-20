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
    public class DsgTotalAllocationChangeVariationStrategyTests : VariationStrategyTestBase
    {
        private DsgTotalAllocationChangeVariationStrategy _variationStrategy;
        
        [TestInitialize]
        public void SetUp()
        {
            _variationStrategy = new DsgTotalAllocationChangeVariationStrategy();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfCurrentProviderClosed()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.PriorState.Provider.Status = Closed);

            await WhenTheVariationsAreProcessed();
            
            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
        } 
        
        [TestMethod]
        public async Task FailsPreconditionCheckIfUpdatedProviderClosed()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = Closed);
            
            await WhenTheVariationsAreProcessed();
            
            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
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
                .QueuedChanges
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task PassesPreconditionCheckEvenIfTotalAllocationNotChanged()
        {
            GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.PriorState.TotalFunding = 100;
                _.RefreshState.TotalFunding = 100;
                _.PriorState.Provider.Status = NewRandomString();
                _.UpdatedProvider.Status = NewRandomString();
            });
            
            await WhenTheVariationsAreProcessed();

            ThenTheVariationChangeWasQueued<AdjustDsgProfilesForUnderOverPaymentChange>();
            AndTheVariationChangeWasQueued<ReAdjustFundingValuesForProfileValuesChange>();  
        }
        
        [TestMethod]
        public async Task QueuesZeroRemainingProfileAndCloseProviderChangesIfPassesPreconditionChecks()
        {
            GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.PriorState.Provider.Status = NewRandomString();
                _.UpdatedProvider.Status = NewRandomString();
                _.PriorState.TotalFunding = 10000M;
            });
            
            await WhenTheVariationsAreProcessed();

            ThenTheVariationChangeWasQueued<AdjustDsgProfilesForUnderOverPaymentChange>();
            AndTheVariationChangeWasQueued<ReAdjustFundingValuesForProfileValuesChange>();
        }

        private async Task WhenTheVariationsAreProcessed()
        {
            await _variationStrategy.Process(VariationContext, null);
        }
    }
}