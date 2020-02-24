using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class FundingUpdatedVariationStrategyTests : VariationStrategyTestBase
    {
        private FundingUpdatedVariationStrategy _variationStrategy;

        [TestInitialize]
        public void SetUp()
        {
            _variationStrategy = new FundingUpdatedVariationStrategy();
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
            VariationContext
                .VariationReasons
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
            VariationContext
                .VariationReasons
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
            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }
        
        [TestMethod]
        public async Task QueuesMetaDataVariationChangedAndRecordsFundingUpdatedIfPassesPreconditionChecks()
        {
            GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.PriorState.Provider.Status = NewRandomString();
                _.UpdatedProvider.Status = NewRandomString();
                _.PriorState.TotalFunding = 10000M;
            });
            
            await WhenTheVariationsAreDetermined();

            ThenTheVariationChangeWasQueued<MetaDataVariationsChange>();
            AndTheVariationReasonsWereRecordedOnTheVariationContext(VariationReason.FundingUpdated);
        }

        private async Task WhenTheVariationsAreDetermined()
        {
            await _variationStrategy.DetermineVariations(VariationContext, null);
        }
    }
}