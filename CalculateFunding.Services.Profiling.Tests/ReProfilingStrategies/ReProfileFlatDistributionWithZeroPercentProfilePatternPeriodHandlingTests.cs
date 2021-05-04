using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Profiling.Tests.ReProfilingStrategies
{
    [TestClass]
    public class ReProfileFlatDistributionWithZeroPercentProfilePatternPeriodHandlingTests : ReProfilingStrategyTest
    {
        [TestInitialize]
        public void SetUp()
        {
            ReProfiling = new ReProfileFlatDistributionWithZeroPercentProfilePatternPeriodHandling();
        }
        
        [TestMethod]
        [DynamicData(nameof(OverAndUnderPaymentExamples), DynamicDataSourceType.Method)]
        public void BundlesUnderAndOverPaymentsEvenlyAcrossTheRemainingFundingLinePeriodsAndCarriesOverRemainingOverPayments(int variationPointerIndex,
            decimal[] originalPeriodValues,
            decimal[] latestPeriodValues,
            decimal[] profilePattern,
            decimal totalAllocation,
            decimal previousTotalAllocation,
            decimal[] expectedAdjustedPeriodValues,
            decimal? expectedRemainingOverPayment)
        {
            GivenTheLatestProfiling(AsLatestProfiling(latestPeriodValues));
            AndTheExistingProfilePeriods(AsExistingProfilePeriods(originalPeriodValues.Take(variationPointerIndex).ToArray()));
            AndTheProfilePattern(NewFundingStreamPeriodProfilePattern(_ => _.WithProfilePattern(AsProfilePattern(profilePattern))));
            
            AndThePreviousFundingTotal(previousTotalAllocation);
            AndTheLatestFundingTotal(totalAllocation);
            
            WhenTheFundingLineIsReProfiled();
            
            AndTheFundingLinePeriodAmountsShouldBe(expectedAdjustedPeriodValues);
            AndTheCarryOverShouldBe(expectedRemainingOverPayment);
        }

        private static IEnumerable<object[]> OverAndUnderPaymentExamples()
        {
            //examples where works exactly like existing flat distribution re profiling (i.e. all periods are 0%)
            // >> If there are no non zero periods left which are unpaid, then use the existing functionality to distribute against all remaining periods.
            yield return new object[]
            {
                5,
                NewDecimals(2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000),
                NewDecimals(1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.37M),
                NewDecimals(1M, 1M, 1M, 1M, 1M, 0M, 0M, 0M, 0M, 0M, 0M, 0M),
                20500M,
                24000M,
                NewDecimals(2000, 2000, 2000, 2000, 2000, 1499.99M, 1499.99M, 1499.99M, 1499.99M, 1499.99M, 1499.99M, 1500.06M),
                null
            };
            yield return new object[]
            {
                8,
                NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000),
                NewDecimals(1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.37M),
                NewDecimals(1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 0M, 0M, 0M, 0M),
                22000M,
                36000M,
                NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 0, 0, 0, 0),
                2000M
            };
            //Examples where it skips unpaid periods which have 0% in the profile pattern periods
            yield return new object[]
            {
                5,
                NewDecimals(2400, 4800, 2400, 4800, 2400, 0, 4800, 0, 2400, 0, 0, 0),
                NewDecimals(2050, 4100, 2050, 4100, 2050, 0, 4100, 0, 2050, 0, 0, 0),
                NewDecimals(10M, 20M, 10M, 20M, 10M, 0M, 20M, 0M, 10M, 0M, 0M, 0M),
                20500M,
                24000M,
                NewDecimals(2400, 4800, 2400, 4800, 2400, 0M, 2875M, 0M, 825M, 0M, 0M, 0M),
                null
            };
            yield return new object[]
            {
                5,
                NewDecimals(2050, 4100, 2050, 4100, 2050, 0, 4100, 0, 2050, 0, 0, 0),
                NewDecimals(2400, 4800, 2400, 4800, 2400, 0, 4800, 0, 2400, 0, 0, 0),
                NewDecimals(10M, 20M, 10M, 20M, 10M, 0M, 20M, 0M, 10M, 0M, 0M, 0M),
                24000M,
                20500M,
                NewDecimals(2050, 4100, 2050, 4100, 2050, 0M, 6025M, 0M, 3625M, 0M, 0M, 0M),
                null
            };
            yield return new object[]
            {
                5,
                NewDecimals(2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 0),
                NewDecimals(1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.37M, 0),
                NewDecimals(1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 0M),
                20500M,
                24000M,
                NewDecimals(2000, 2000, 2000, 2000, 2000, 1499.99M, 1499.99M, 1499.99M, 1499.99M, 1499.99M, 1499.99M, 1500.06M, 0),
                null
            };
        }
    }
}