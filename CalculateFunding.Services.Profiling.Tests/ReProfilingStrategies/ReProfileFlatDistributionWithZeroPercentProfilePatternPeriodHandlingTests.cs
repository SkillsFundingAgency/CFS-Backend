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
        [DynamicData(nameof(RemainingFundingExamples), DynamicDataSourceType.Method)]
        public void BundlesUnderAndOverPaymentsAcrossTheFundingLinePeriodsAndCarriesOverRemainingOverPayments(int variationPointerIndex,
            decimal[] originalPeriodValues,
            decimal[] newTheoreticalPeriodValues,
            decimal[] profilePattern,
            decimal[] expectedAdjustedPeriodValues)
        {
            Context.Request.MidYearType = Models.MidYearType.Opener;

            GivenTheLatestProfiling(AsLatestProfiling(newTheoreticalPeriodValues));
            AndTheExistingProfilePeriods(AsExistingProfilePeriods(originalPeriodValues.Take(variationPointerIndex).ToArray()));
            AndTheLatestFundingTotal(newTheoreticalPeriodValues.Sum());
            AndTheProfilePattern(NewFundingStreamPeriodProfilePattern(_ => _.WithProfilePattern(AsProfilePattern(profilePattern))));

            WhenTheFundingLineIsReProfiled();

            AndTheFundingLinePeriodAmountsShouldBe(expectedAdjustedPeriodValues);
            AndTheCarryOverShouldBe(0);
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
            decimal? expectedRemainingOverPayment,
            bool useVariationIndex)
        {
            GivenTheLatestProfiling(AsLatestProfiling(latestPeriodValues));

            if (useVariationIndex)
            {
                AndTheExistingProfilePeriods(AsExistingProfilePeriods(originalPeriodValues.ToArray()));
                AndTheVariationPointerIndex(variationPointerIndex);
            }
            else
            {
                AndTheExistingProfilePeriods(AsExistingProfilePeriods(originalPeriodValues.Take(variationPointerIndex).ToArray()));
            }
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
                null,
                false
            };
            yield return new object[]
            {
                8,
                NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000),
                NewDecimals(1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.37M),
                NewDecimals(1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 0M, 0M, 0M, 0M),
                22000M,
                36000M,
                NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, -500.01M, -500.01M, -500.01M, -499.97M),
                null,
                false
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
                null,
                false
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
                null,
                false
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
                null,
                true
            };
            //example showing that we move remainder from final period to final none zero period to account for profiling code being incorrect for none zero periods
            yield return new object[]
            {
                5,
                NewDecimals(2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 0),
                //the final period from the profiling code here illustrates a case where the remainder is dumped in the final period where it is zer0 % (so incorrectly)
                NewDecimals(1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.37M, 2),
                NewDecimals(1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 0M),
                20502M,
                24000M,
                //the re profiling code locates the remainder in the final period as incorrect and moves it to the final none zero percent period
                NewDecimals(2000, 2000, 2000, 2000, 2000, 1499.99M, 1499.99M, 1499.99M, 1499.99M, 1499.99M, 1499.99M, 1502.06M, 0),
                null,
                true
            };
            yield return new object[]
            {
                5,
                NewDecimals(2000, 2000.01M, 1999.99M, 2000, 2000, 2000.01M, 1999.99M, 2000, 2000, 2000, 2000, 2000, 0),
                NewDecimals(2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 0),
                NewDecimals(1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 0M),
                24000M,
                24000M,
                NewDecimals(2000, 2000.01M, 1999.99M, 2000, 2000, 2000.01M, 1999.99M, 2000, 2000, 2000, 2000, 2000, 0),
                null,
                true
            };
        }

        private static IEnumerable<object[]> RemainingFundingExamples()
        {
            yield return new object[]
            {
                3,
                NewDecimals(1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000),
                NewDecimals(1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100),
                NewDecimals(1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M),
                NewDecimals(0, 0, 0, 1571.43M, 1571.43M, 1571.43M, 1571.43M, 1571.43M, 1571.43M, 1571.42M),
            };
            yield return new object[]
            {
                6,
                NewDecimals(1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100),
                NewDecimals(950, 950, 950, 950, 950, 950, 950, 950, 950, 950),
                NewDecimals(1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M),
                NewDecimals(0, 0, 0, 0, 0, 0, 2375, 2375, 2375, 2375),
            };
            yield return new object[]
            {
                9,
                NewDecimals(1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000),
                NewDecimals(800, 800, 800, 800, 800, 800, 800, 800, 800, 800),
                NewDecimals(1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M, 1M),
                NewDecimals(0, 0, 0, 0, 0, 0, 0, 0, 0, 8000),
            };
            yield return new object[]
            {
                3,
                NewDecimals(0, 0, 1000, 0, 0, 1000, 0, 0, 0, 1000),
                NewDecimals(0, 0, 800, 0, 0, 800, 0, 0, 0, 800),
                NewDecimals(0M, 0M, 1M, 0M, 0M, 1M, 0M, 0M, 0M, 1M),
                NewDecimals(0, 0, 0, 0, 0, 1200, 0, 0, 0, 1200),
            };
            yield return new object[]
            {
                3,
                NewDecimals(0, 0, 1000, 0, 0, 1000, 0, 0, 0),
                NewDecimals(0, 0, 800, 0, 0, 800, 0, 0, 0, 800),
                NewDecimals(0M, 0M, 1M, 0M, 0M, 1M, 0M, 0M, 0M, 1M),
                NewDecimals(0, 0, 0, 0, 0, 1200, 0, 0, 0, 1200),
            };
            // Example 32
            //  1619
            //      Mid year SSF / College to Academy converter - Three payment profile - 1 default payment month in the past
            yield return new object[]
            {
                4,
                NewDecimals(0, 13958.64M, 0, 0, 0, 0, 0, 0, 13958.64M, 13959.05M, 0, 0),
                NewDecimals(0, 9305.90M, 0, 0, 0, 0, 0, 0, 9305.90M, 9305.89M, 0, 0),
                NewDecimals(0M, 1M, 0M, 0M, 0M, 0M, 0M, 0M, 1M, 1M, 0, 0),
                NewDecimals(0, 0, 0, 0, 0, 0, 0, 0, 13958.85M, 13958.84M, 0, 0),
            };
        }
    }
}