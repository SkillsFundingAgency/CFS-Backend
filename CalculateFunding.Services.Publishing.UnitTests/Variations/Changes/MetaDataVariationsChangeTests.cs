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
    public class MetaDataVariationsChangeTests : VariationChangeTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            Change = new MetaDataVariationsChange(VariationContext, "ProviderMetadata");
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

            VariationsApplication
                .Received(1)
                .AddPublishedProviderToUpdate(VariationContext.PublishedProvider);
        }

        private void GivenTheVariationReasons(params VariationReason[] variationReasons)
        {
            VariationContext.VariationReasons = variationReasons;
        }
        
        private VariationReason NewRandomVariationReason() => new RandomEnum<VariationReason>();
    }
}