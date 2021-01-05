using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class ProfilingUpdatedVariationStrategyTests : ProfilingVariationStrategyTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            VariationStrategy = new ProfilingUpdatedVariationStrategy();
        }

        [TestMethod]
        public async Task AddsProfilingUpdatedVariationReasonAndQueuesMetaDataVariationChangeIfProfilingDiffers()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = NewRandomString());
            AndTheRefreshStateFundingLines(NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                .WithFundingLineType(FundingLineType.Payment)
                .WithValue(new RandomNumberBetween(1, int.MaxValue))
                .WithDistributionPeriods(NewDistributionPeriod(dp =>
                dp.WithProfilePeriods(NewProfilePeriod())))));

            await WhenTheVariationsAreDetermined();

            ThenTheVariationChangeWasQueued<MetaDataVariationsChange>();
            AndTheVariationReasonsWereRecordedOnTheVariationContext(VariationReason.ProfilingUpdated);
        }
    }
}