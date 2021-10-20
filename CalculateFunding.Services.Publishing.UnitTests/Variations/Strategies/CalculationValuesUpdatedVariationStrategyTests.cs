using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class CalculationValuesUpdatedVariationStrategyTests : VariationStrategyTestBase
    {
        private IVariationStrategy _variationStrategy;

        [TestInitialize]
        public void SetUp()
        {
            _variationStrategy = new CalculationValuesUpdatedVariationStrategy();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfProviderPreviouslyClosed()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.RefreshState.Calculations = 
                new List<FundingCalculation> {
                    NewFundingCalculation(fc => fc
                        .WithTemplateCalculationId(_.PriorState.Calculations.FirstOrDefault().TemplateCalculationId)
                        .WithValue(NewRandomString()))});

            await WhenTheVariationsAreProcessed();

            VariationContext
                .ErrorMessages
                .Should()
                .BeEmpty();

            ThenTheVariationChangeWasQueued<MetaDataVariationsChange>();
            AndTheVariationReasonsWereRecordedOnTheVariationContext(VariationReason.CalculationValuesUpdated);
        }

        private async Task WhenTheVariationsAreProcessed()
        {
            await _variationStrategy.Process(VariationContext, null);
        }
    }
}
