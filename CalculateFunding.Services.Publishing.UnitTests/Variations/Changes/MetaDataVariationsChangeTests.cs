using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class MetaDataVariationsChangeTests : ProviderVariationContextTestBase
    {
        private MetaDataVariationsChange _change;
        private IApplyProviderVariations _applyProviderVariations;

        [TestInitialize]
        public void SetUp()
        {
            _change = new MetaDataVariationsChange(VariationContext);

            _applyProviderVariations = Substitute.For<IApplyProviderVariations>();
        }
        
        [TestMethod]
        public async Task CopiesVariationReasonsOntoRefreshState()
        {
            VariationReason[] expectedVariationReasons =
            {
                NewRandomVariationReason(),
                NewRandomVariationReason(),
                NewRandomVariationReason(),
                NewRandomVariationReason(),
            };
            
            GivenTheVariationReasons(expectedVariationReasons);

            await WhenTheChangeIsApplied();

            VariationContext
                .RefreshState
                .VariationReasons
                .Should()
                .BeEquivalentTo(expectedVariationReasons);
            
            _applyProviderVariations
                .Received(1)
                .AddPublishedProviderToUpdate(VariationContext.PublishedProvider);
        }
        
        private async Task WhenTheChangeIsApplied()
        {
            await _change.Apply(_applyProviderVariations);
        }

        private void GivenTheVariationReasons(params VariationReason[] variationReasons)
        {
            VariationContext.Result.VariationReasons = variationReasons;
        }
        
        private VariationReason NewRandomVariationReason() => new RandomEnum<VariationReason>();
    }
}