using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Profiling.Tests.ReProfilingStrategies
{
    [TestClass]
    public class ReProfileFutureDistributionPeriodsWithAdjustmentsTests : ReProfilingStrategyTest
    {
        [TestInitialize]
        public void SetUp()
        {
            ReProfiling = new ReProfileFutureDistributionPeriodsWithAdjustments();
        }

        [TestMethod]
        [DynamicData(nameof(IncreaseOrDecreaseFundingExamples), DynamicDataSourceType.Method)]
        public void BundlesIncreaseOrDecreaseFundingAdjustsFutureDistributionPeriodPaymentsWithoutAnyCarryOverPayments(int variationPointerIndex,
            decimal[] originalPeriodValues,
            decimal totalAllocation,
            decimal previousTotalAllocation,
            decimal[] expectedAdjustedPeriodValues,
            decimal? expectedRemainingOverPayment)
        {
            GivenTheLatestProfiling(AsLatestProfiling(originalPeriodValues));
            GivenTheExistingProfilePeriods(AsExistingProfilePeriods(originalPeriodValues.Take(variationPointerIndex).ToArray()));

            AndThePreviousFundingTotal(previousTotalAllocation);
            AndTheLatestFundingTotal(totalAllocation);

            WhenTheFundingLineIsReProfiled();

            ThenTheFundingLinePeriodAmountsShouldBe(expectedAdjustedPeriodValues);
            AndTheCarryOverShouldBe(expectedRemainingOverPayment);
        }

        private static IEnumerable<object[]> IncreaseOrDecreaseFundingExamples()
        {
            // Example 1 - increase funding, catchup next period
            yield return new object[]
            {
                2,
                NewAmounts(1000, 1000, 1000, 1000, 1000),
                6000M,
                5000M,
                NewAmounts(1000, 1000, 1600, 1200, 1200),
                0M
            };
            // Example 2 - decrease funding
            yield return new object[]
            {
                2,
                NewAmounts(1000, 1000, 1000, 1000, 1000),
                4000M,
                5000M,
                NewAmounts(1000, 1000, 400, 800, 800),
                0M
            };
            //Example 3 - decrease funding more than period amount
            yield return new object[]
            {
                 2,
                 NewAmounts(1000, 1000, 1000, 1000, 1000),
                 3000M,
                 5000M,
                 NewAmounts(1000, 1000, -200, 600, 600),
                 0M
            };
            // Example 4 - decrease funding to paid amount -zero off
            yield return new object[]
            {
                2,
                NewAmounts(1000, 1000, 1000, 1000, 1000),
                2000M,
                5000M,
                NewAmounts(1000, 1000, 0, 0, 0),
                0M
            };
            // Example 5 - decrease funding more than paid amount
            yield return new object[]
            {
                2,
                NewAmounts(1000, 1000, 1000, 1000, 1000),
                1000M,
                5000M,
                NewAmounts(1000, 1000, -1000, 0, 0),
                0M
            };
            // Example 6 - increase of funding, with different percentages in remaining periods
            yield return new object[]
            {
                2,
                NewAmounts(750, 1000, 2000, 500, 750),
                6000M,
                5000M,
                NewAmounts(750, 1000, 2750, 600, 900),
                0M
            };
            // Example 7 - decimals
            yield return new object[]
            {
                2,
                NewAmounts(1000, 1000, 1000, 1000, 1000),
                5169.76M,
                5000M,
                NewAmounts(1000, 1000, 1101.86M, 1033.95M, 1033.95M),
                0M
            };
        }
    }
}
