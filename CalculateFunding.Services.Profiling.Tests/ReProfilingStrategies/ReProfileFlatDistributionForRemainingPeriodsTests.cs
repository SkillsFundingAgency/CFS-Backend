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
            // //Example 3 - Provider overpaid - recovery of overpayment spread over remaining instalments - negated in funding period without reduction to zero
            // yield return new object[]
            // {
            //     8,
            //     NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000),
            //     33600M,
            //     36000M,
            //     NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 2400, 2400, 2400, 2400),
            //     null
            // };
            // //Example 4 - Provider overpaid - recovery of overpayment spread over remaining instalments - negated in funding period through reduction to zero
            // yield return new object[]
            // {
            //     8,
            //     NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000),
            //     24000M,
            //     36000M,
            //     NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 0, 0, 0, 0),
            //     null
            // };
            //Example 5 - Provider overpaid - recovery of overpayment spread over remaining instalments with balance to carry forward
            yield return new object[]
            {
                8,
                NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000),
                NewDecimals(1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.33M, 1833.37M),
                22000M,
                36000M,
                NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 0, 0, 0, 0),
                2000M
            };
            #endregion
            
            yield break;
            
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
                NewDecimals(0, 0, 0, 18735.57M, 0, 0, 0, 0, 0, 0, 0, 0),
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
                NewDecimals(0, 0, 0, 22542.43M, 0, 0, 0, 0, 0, 0, 0, 0),
                null
            };
            //Example 3 - Mid year allocation change - no over or under payment (flat distribution) - single payment profile - payment in the future
            yield return new object[]
            {
                1,
                NewDecimals(0, 0, 0, 100, 0, 0, 0, 0, 0, 0, 0, 0),
                NewDecimals(0, 0, 0, 18735.57M, 0, 0, 0, 0, 0, 0, 0, 0),
                22542.43M,
                18735.57M,
                NewDecimals(0, 0, 0, 22542.43M, 0, 0, 0, 0, 0, 0, 0, 0),
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
                NewDecimals(0, 0, 0, 21962.8M, 0, 0, 0, 15689.87M, 0, 0, 0, 0),
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
                NewDecimals(12491M, 0, 0, 0, 0, 0, 0, 5425.61M, 0, 0, 0, 0),
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
                NewDecimals(0, 0, 0, 12632.66M, 0, 0, 0, 9024.56M, 0, 0, 0, 0),
                null
            };
            //Example 7 - Mid year allocation change - no over or under payment (flat distribution) - three payment profile - two payments in the past, one in the future
            yield return new object[]
            {
                9,
                NewDecimals(0, 33.333M, 0, 0, 0, 0, 0, 0, 33.333M, 33.333M, 0, 0),
                NewDecimals(0, 12550.76M, 0, 0, 0, 0, 0, 12550.76M, 12551.15M, 0, 0, 0),
                37652.67M,
                29674.22M,
                NewDecimals(0, 12550.76M, 0, 0, 0, 0, 0, 12550.76M, 9891.31M, 0, 0, 0),
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
                NewDecimals(0, 13847.19M, 0, 0, 0, 0, 0, 13847.19M, 13847.6M, 0, 0, 0),
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
                NewDecimals(9413.17M, 0, 9413.17M, 0, 0, 9413.17M, 0, 0, 9413.16M, 0, 0, 0),
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
                NewDecimals(4729.18M, 3554.41M, 3599.6M, 2801.36M, 2108.55M, 2389.62M, 2218.93M, 2201.86M, 5359.58M, 4949.93M, 4096.49M, 2423.75M),
                null
            };
            //Example 11 - Mid year allocation change - no over or under payment (flat distribution) - variable profile - some default payment months in the past, others still in the future
            yield return new object[]
            {
                3,
                NewDecimals(8, 7.97M, 9.66M, 10.83M, 7.69M, 9.34M, 9.2M, 8.2M, 7.94M, 7.54M, 7.2M, 6.43M),
                NewDecimals(3012.21M, 3000.92M, 3637.25M, 4077.78M, 2895.49M, 3516.76M, 3464.05M, 3087.52M, 2989.62M, 2839.01M, 2710.99M, 2421.07M),
                37652.67M,
                30876.52M,
                NewDecimals(3012.21M, 3000.92M, 3637.25M, 4077.78M, 2895.49M, 3516.76M, 3464.05M, 3087.52M, 2989.62M, 2839.01M, 2710.99M, 2421.07M),
                null
            };
            //Example 12 - Mid year allocation change - no over or under payment (flat distribution) - variable profile - some default payment months in the past, others still in the future
            yield return new object[]
            {
                8,
                NewDecimals(2.2222222M, 2.77777778M, 8.33333333333333M, 8.33333333333333M, 8.33333333333333M, 10.5555555555556M, 8.88888888888889M, 8.88888888888889M, 11.1111111111111M, 10.5555555555556M, 10M, 10M),
                NewDecimals(836.73M, 1045.91M, 3137.72M, 3137.72M, 3137.72M, 3974.45M, 3346.9M, 3346.9M, 4183.63M, 3974.45M, 3765.27M, 3765.27M),
                37652.67M,
                46873.12M,
                NewDecimals(836.73M, 1045.91M, 3137.72M, 3137.72M, 3137.72M, 3974.45M, 3346.9M, 3346.9M, 4183.63M, 3974.45M, 3765.27M, 3765.27M),
                null
            };
            
            #endregion
            
        }
    }
}