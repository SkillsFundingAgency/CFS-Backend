using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class DsgTotalAllocationChangeVariationStrategyTests : VariationStrategyTestBase
    {
        private const string Closed = "Closed";
        
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

            await WhenTheVariationsAreDetermined();
            
            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
        } 
        
        [TestMethod]
        public async Task FailsPreconditionCheckIfUpdatedProviderClosed()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = Closed);
            
            await WhenTheVariationsAreDetermined();
            
            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfTotalAllocationNotChanged()
        {
            GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.PriorState.TotalFunding = 100;
                _.RefreshState.TotalFunding = 100;
            });
            
            await WhenTheVariationsAreDetermined();
            
            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();   
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
            
            await WhenTheVariationsAreDetermined();

            ThenTheVariationChangeWasQueued<AdjustDsgProfilesForUnderOverPaymentChange>();
            AndTheVariationChangeWasQueued<ReAdjustFundingValuesForProfileValuesChange>();
        }

        private async Task WhenTheVariationsAreDetermined()
        {
            await _variationStrategy.DetermineVariations(VariationContext, null);
        }
    }
}