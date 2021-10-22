using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Profiling.Tests.ReProfilingStrategies
{
    [TestClass]
    public class ReProfileFlatDistributionForRemainingPeriodsTests : ReProfilingStrategyTest
    {
        [TestInitialize]
        public void SetUp()
        {
            ReProfiling = new ReProfileFlatDistributionForRemainingPeriods();
        }
        
        [TestMethod]
        [DynamicData(nameof(OverAndUnderPaymentExamples), DynamicDataSourceType.Method)]
        public void BundlesUnderAndOverPaymentsEvenlyAcrossTheRemainingFundingLinePeriodsAndCarriesOverRemainingOverPayments(int variationPointerIndex,
            decimal[] originalPeriodValues,
            decimal[] latestPeriodValues,
            decimal totalAllocation,
            decimal previousTotalAllocation,
            decimal[] expectedAdjustedPeriodValues,
            decimal? expectedRemainingOverPayment)
        {
            //just need something in the refresh lines though we throw it all away during the re profiling
            GivenTheLatestProfiling(AsLatestProfiling(latestPeriodValues));
            AndTheExistingProfilePeriods(AsExistingProfilePeriods(originalPeriodValues.Take(variationPointerIndex).ToArray()));
            AndTheProfilePattern(NewFundingStreamPeriodProfilePattern());
            
            AndThePreviousFundingTotal(previousTotalAllocation);
            AndTheLatestFundingTotal(totalAllocation);
            
            WhenTheFundingLineIsReProfiled();
            
            AndTheFundingLinePeriodAmountsShouldBe(expectedAdjustedPeriodValues);
            AndTheCarryOverShouldBe(expectedRemainingOverPayment);
        }

        private static IEnumerable<object[]> OverAndUnderPaymentExamples()
        {
            #region original_examples
            //Example 2 - Provider overpaid - recovery of overpayment spread over remaining instalments
            //NB - the uncommented examples in this section have been changed to work correctly with the latest allocation having been profiled prior to
            //re profiling to give a flat distribution of the adjustment needed to keep with in the current allocation taking payments already made into account
            //so this now works like DSG except that it uses a flat distribution of the difference rather than catch up fully in the next available periods
            yield return new object[]
            {
                5,
                NewDecimals(2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000),
                NewDecimals(1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.33M, 1708.37M),
                20500M,
                24000M,
                NewDecimals(2000, 2000, 2000, 2000, 2000, 1499.99M, 1499.99M, 1499.99M, 1499.99M, 1499.99M, 1499.99M, 1500.06M),
                null
            };

            //Example 3 - Provider overpaid - recovery of overpayment spread over remaining instalments - negated in funding period without reduction to zero
            yield return new object[]
             {
                 8,
                 NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000),
                 NewDecimals(2800, 2800, 2800, 2800, 2800, 2800, 2800, 2800, 2800, 2800, 2800, 2800),
                 33600M,
                 36000M,
                 NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 2400, 2400, 2400, 2400),
                 null
             };
            //Example 4 - Provider overpaid - recovery of overpayment spread over remaining instalments - negated in funding period through reduction to zero
             yield return new object[]
             {
                 8,
                 NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000),
                 NewDecimals(2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000),
                 24000M,
                 36000M,
                 NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 0, 0, 0, 0),
                 null
             };
            //Example 5 - Provider overpaid - recovery of overpayment spread over remaining instalments with balance to carry forward
            yield return new object[]
            {
                8,
                NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000),
                NewDecimals(1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.37M),
                22000M,
                36000M,
                NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, -500.01M, -500.01M, -500.01M, -499.97M),
                null
            };
            #endregion

            //NB these examples are not correct so banging out of the tests here for now
            //NB the examples below use profile percentages in the first line but these should be latest profiled value for the way the tests have been constructed now for the fix
            #region differing_percentages_and_rounding_examples 
            //Example 1 - Mid year allocation change - no over or under payment (flat distribution) - single payment profile - payment in the past
            yield return new object[]
            {
                7,
                NewDecimals(0, 0, 0, 100, 0, 0, 0, 0, 0, 0, 0, 0),
                NewDecimals(0, 0, 0, 18735.57M, 0, 0, 0, 0, 0, 0, 0, 0),
                22542.43M,
                18735.57M,
                NewDecimals(0, 0, 0, 100, 0, 0, 0, 4488.49M, 4488.49M, 4488.49M, 4488.49M, 4488.47M),
                null
            };
            //Example 2 - Mid year allocation change - no over or under payment (flat distribution) - single payment profile - payment in the variation month
            yield return new object[]
            {
                3,
                NewDecimals(0, 0, 0, 100, 0, 0, 0, 0, 0, 0, 0, 0),
                NewDecimals(0, 0, 0, 18735.57M, 0, 0, 0, 0, 0, 0, 0, 0),
                22542.43M,
                18735.57M,
                NewDecimals(0, 0, 0, 19158.55M, 422.98M, 422.98M, 422.98M, 422.98M, 422.98M, 422.98M, 422.98M, 423.02M),
                null
            };
            //Example 4 - Mid year allocation change - no over or under payment (flat distribution) - two payment profile - default payment months both in the past
            yield return new object[]
            {
                8,
                NewDecimals(0, 0, 0, 58.33M, 0, 0, 0, 41.67M, 0, 0, 0, 0),
                NewDecimals(0, 0, 0, 21962.8M, 0, 0, 0, 15689.87M, 0, 0, 0, 0),
                37652.67M,
                35276.22M,
                NewDecimals(0, 0, 0, 58.33M, 0, 0, 0, 41.67M, 9388.17M, 9388.17M, 9388.17M, 9388.16M),
                null
            };
            //Example 5 - Mid year allocation change - no over or under payment (flat distribution) - two payment profile - 1 default payment month in the past, 1 default payment month in the future
            yield return new object[]
            {
                4,
                NewDecimals(66.67M, 0, 0, 0, 0, 0, 0, 33.33M, 0, 0, 0, 0),
                NewDecimals(12491M, 0, 0, 0, 0, 0, 0, 6244.57M, 0, 0, 0, 0),
                18735.57M,
                16278.45M,
                NewDecimals(66.67M, 0, 0, 0, 1553.04M, 1553.04M, 1553.04M, 7797.61M, 1553.04M, 1553.04M, 1553.04M, 1553.05M),
                null
            };
            //Example 6 - Mid year allocation change - no over or under payment (flat distribution) - two payment profile - default payment months both in the future
            yield return new object[]
            {
                1,
                NewDecimals(0, 0, 0, 58.33M, 0, 0, 0, 41.67M, 0, 0, 0, 0),
                NewDecimals(0, 0, 0, 10928.46M, 0, 0, 0, 7807.11M, 0, 0, 0, 0),
                18735.57M,
                21657.22M,
                NewDecimals(0, 0, 0, 10928.46M, 0, 0, 0, 7807.11M, 0, 0, 0, 0),
                null
            };
            //Example 7 - Mid year allocation change - no over or under payment (flat distribution) - three payment profile - two payments in the past, one in the future
            yield return new object[]
            {
                9,
                NewDecimals(0, 33.33M, 0, 0, 0, 0, 0, 0, 33.33M, 33.33M, 0, 0),
                NewDecimals(0, 12550.76M, 0, 0, 0, 0, 0, 12550.76M, 12551.15M, 0, 0, 0),
                37652.67M,
                29674.22M,
                NewDecimals(0, 33.33M, 0, 0, 0, 0, 0, 0, 33.33M, 12528.67M, 12528.67M, 12528.67M),
                null
            };
            //Example 8 - Mid year allocation change - no over or under payment (flat distribution) - three payment profile - all payments in the future
            yield return new object[]
            {
                0,
                NewDecimals(0, 33.333M, 0, 0, 0, 0, 0, 0, 33.333M, 33.333M, 0, 0),
                NewDecimals(0, 12550.76M, 0, 0, 0, 0, 0, 12550.76M, 12551.15M, 0, 0, 0),
                37652.67M,
                41541.98M,
                NewDecimals(0, 12550.76M, 0, 0, 0, 0, 0, 12550.76M, 12551.15M, 0, 0, 0),
                null
            };
            //Example 9 - Mid year allocation change - no over or under payment (flat distribution) - variable profile - all default payment months in the past
            yield return new object[]
            {
                9,
                NewDecimals(25, 0, 25, 0, 0, 25, 0, 0, 25, 0, 0, 0),
                NewDecimals(9413.17M, 0, 9413.17M, 0, 0, 9413.17M, 0, 0, 9413.16M, 0, 0, 0),
                37652.67M,
                31765.45M,
                NewDecimals(25, 0, 25, 0, 0, 25, 0, 0, 25, 12517.56M, 12517.56M, 12517.55M),
                null
            };
            //Example 10 - Mid year allocation change - no over or under payment (flat distribution) - variable profile - some default payment months in the past, others still in the future
            yield return new object[]
            {
                5,
                NewDecimals(12.56M, 9.44M, 9.56M, 7.44M, 5.6M, 5.6M, 5.2M, 5.16M, 12.56M, 11.6M, 9.6M, 5.68M),
                NewDecimals(4729.18M, 3554.41M, 3599.6M, 2801.36M, 2108.55M, 2108.55M, 1957.94M, 1942.88M, 4729.18M, 4367.71M, 3614.66M, 2138.65M),
                37652.67M,
                42671.78M,
                NewDecimals(12.56M, 9.44M, 9.56M, 7.44M, 5.6M, 4501.19M, 4350.58M, 4335.52M, 7121.82M, 6760.35M, 6007.30M, 4531.31M),
                null
            };
            #endregion
            
        }
    }
}