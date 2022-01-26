using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Profiling.Models;
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
            decimal[] newTheoreticalProfilePeriods,
            decimal[] originalPeriodValues,
            decimal totalAllocation,
            decimal previousTotalAllocation,
            decimal[] expectedAdjustedPeriodValues,
            decimal? expectedRemainingOverPayment,
            MidYearType? midYearType,
            bool alreadyPaidUpToIndex)
        {
            Context.Request.MidYearType = midYearType;
            Context.Request.AlreadyPaidUpToIndex = alreadyPaidUpToIndex;

            GivenTheLatestProfiling(AsLatestProfiling(newTheoreticalProfilePeriods));
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
                NewDecimals(1200, 1200, 1200, 1200, 1200),
                NewDecimals(1000, 1000, 1000, 1000, 1000),
                6000M,
                5000M,
                NewDecimals(1000, 1000, 1600, 1200, 1200),
                0M,
                null,
                false
            };
            // Example 2 - decrease funding
            yield return new object[]
            {
                2,
                NewDecimals(800, 800, 800, 800, 800),
                NewDecimals(1000, 1000, 1000, 1000, 1000),
                4000M,
                5000M,
                NewDecimals(1000, 1000, 400, 800, 800),
                0M,
                null,
                false
            };
            //Example 3 - decrease funding more than period amount
            yield return new object[]
            {
                 2,
                 NewDecimals(600, 600, 600, 600, 600),
                 NewDecimals(1000, 1000, 1000, 1000, 1000),
                 3000M,
                 5000M,
                 NewDecimals(1000, 1000, -200, 600, 600),
                 0M,
                null,
                false
            };
            // Example 4 - decrease funding to paid amount -zero off
            yield return new object[]
            {
                2,
                NewDecimals(400, 400, 400, 400, 400),
                NewDecimals(1000, 1000, 1000, 1000, 1000),
                2000M,
                5000M,
                NewDecimals(1000, 1000, 0, 0, 0),
                0M,
                null,
                false
            };
            // Example 5 - decrease funding more than paid amount
            yield return new object[]
            {
                2,
                NewDecimals(200, 200, 200, 200, 200),
                NewDecimals(1000, 1000, 1000, 1000, 1000),
                1000M,
                5000M,
                NewDecimals(1000, 1000, -1000, 0, 0),
                0M,
                null,
                false
            };
            // Example 6 - increase of funding, with different percentages in remaining periods
            yield return new object[]
            {
                2,
                NewDecimals(900, 1200, 2400, 600, 900),
                NewDecimals(750, 1000, 2000, 500, 750),
                6000M,
                5000M,
                NewDecimals(750, 1000, 2750, 600, 900),
                0M,
                null,
                false
            };
            // Example 7 - decimals
            yield return new object[]
            {
                2,
                NewDecimals(1033.95M, 1033.95M, 1033.95M, 1033.95M, 1033.95M),
                NewDecimals(1000, 1000, 1000, 1000, 1000),
                5169.76M,
                5000M,
                NewDecimals(1000, 1000, 1101.86M, 1033.95M, 1033.95M),
                0M,
                null,
                false
            };
            // Example 8 - decimals
            yield return new object[]
            {
                2,
                NewDecimals(600M, 600M, 600M, 600M, 600M),
                NewDecimals(0, 0, 0, 0, 0),
                3000M,
                0M,
                NewDecimals(0, 0, 1800M, 600M, 600M),
                0M,
                null,
                false
            };
            // Example 9 - negative funding lines
            yield return new object[]
            {
                2,
                NewDecimals(-1033.95M, -1033.95M, -1033.95M, -1033.95M, -1033.95M),
                NewDecimals(-1000, -1000, -1000, -1000, -1000),
                -5169.76M,
                -5000M,
                NewDecimals(-1000, -1000, -1101.86M, -1033.95M, -1033.95M),
                0M,
                null,
                false
            };
            // Example 10 - test for mid year opener with catchup
            yield return new object[]
            {
                2,
                NewDecimals(1033.95M, 1033.95M, 1033.95M, 1033.95M, 1033.95M),
                NewDecimals(0, 0, 0, 0, 0),
                5169.76M,
                0M,
                NewDecimals(0, 0, 3101.86M, 1033.95M, 1033.95M),
                0M,
                MidYearType.OpenerCatchup,
                false
            };
            // Example 11
            // 1619
            //      Genuine mid year new opener - Two payment profile - default payment months both in the future
            yield return new object[]
            {
                2,
                NewDecimals(0, 0, 0, 21962.80M, 0, 0, 0, 15689.87M, 0, 0, 0, 0),
                NewDecimals(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                37652.67M,
                0M,
                NewDecimals(0, 0, 0, 21962.80M, 0, 0, 0, 15689.87M, 0, 0, 0, 0),
                0M,
                MidYearType.Opener,
                false
            };
            // Example 27
            //  1619
            //      Genuine mid year new opener - Three payment profile - 1 default payment month in the past
            yield return new object[]
            {
                5,
                NewDecimals(0, 13958.64M, 0, 0, 0, 0, 0, 0, 13958.64M, 13959.05M, 0, 0),
                NewDecimals(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                41876.33M,
                0M,
                NewDecimals(0, 0, 0, 0, 0, 13958.64M, 0, 0, 13958.64M, 13959.05M, 0, 0),
                0M,
                MidYearType.Opener,
                false
            };
            // Example 29
            //  1619
            //      Genuine mid year new opener - Three payment profile - 2 default payment months in the past
            yield return new object[]
            {
                8,
                NewDecimals(0, 13958.64M, 0, 0, 0, 0, 0, 0, 13958.64M, 13959.05M, 0, 0),
                NewDecimals(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                41876.33M,
                0M,
                NewDecimals(0, 0, 0, 0, 0, 0, 0, 0, 27917.28M, 13959.05M, 0, 0),
                0M,
                MidYearType.Opener,
                false
            };
            // Example 38
            //  1619
            //      Apparent / pop-up mid year new opener - Variable profile - some default payment months in the past, 
            //      others still in the future
            yield return new object[]
            {
                6,
                NewDecimals(4631.14M, 3480.73M, 3524.97M, 2743.29M, 2064.84M, 2064.84M, 1917.35M, 1902.60M, 4631.14M, 4277.17M, 3539.72M, 2094.33M),
                NewDecimals(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                36872.12M,
                0M,
                NewDecimals(0, 0, 0, 0, 0, 0, 20427.16M, 1902.60M, 4631.14M, 4277.17M, 3539.72M, 2094.33M),
                0M,
                MidYearType.OpenerCatchup,
                false
            };
            // Example 39
            //  1619
            //      Genuine mid year new opener - Three payment profile - 2 default payment months in the past
            yield return new object[]
            {
                6,
                NewDecimals(4631.14M, 3480.73M, 3524.97M, 2743.29M, 2064.84M, 2064.84M, 1917.35M, 1902.60M, 4631.14M, 4277.17M, 3539.72M, 2094.33M),
                NewDecimals(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                36872.12M,
                0M,
                NewDecimals(0, 0, 0, 0, 0, 0, 20427.16M, 1902.60M, 4631.14M, 4277.17M, 3539.72M, 2094.33M),
                0M,
                MidYearType.Opener,
                false
            };
            // Example 50
            //  1619
            //      Mid year closer - Two payment profile - 1 default payment month in the past, 1 default payment month in the future
            yield return new object[]
            {
                5,
                NewDecimals(0, 0, 0, 16129.22M, 0, 0, 0, 11522.45M, 0, 0, 0, 0),
                NewDecimals(0, 0, 0, 16129.22M, 0, 0, 0, 11522.45M, 0, 0, 0, 0),
                13825.83M,
                27651.67M,
                NewDecimals(0, 0, 0, 16129.22M, 0, -2303.39M, 0, 0, 0, 0, 0, 0),
                0M,
                MidYearType.Closure,
                false
            };
            // Example 53
            //  1619
            //      Mid year closer - Variable profile - all default payment months in the future
            yield return new object[]
            {
                2,
                NewDecimals(0, 0, 0, 2942.69M, 2942.65M, 2942.65M, 2942.65M, 2942.65M, 2942.65M, 2942.65M, 2942.65M, 2942.71M),
                NewDecimals(0, 0, 0, 2942.69M, 2942.65M, 2942.65M, 2942.65M, 2942.65M, 2942.65M, 2942.65M, 2942.65M, 2942.71M),
                6620.99M,
                26483.99M,
                NewDecimals(0, 0, 6620.99M, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                0M,
                MidYearType.Closure,
                false
            };
            // Example 66
            //  1619
            //      Existing provider with new eligibility for a funding line - Single payment profile - default payment month in the past
            yield return new object[]
            {
                5,
                NewDecimals(0, 0, 0, 34288M, 0, 0, 0, 0, 0, 0, 0, 0),
                NewDecimals(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                34288M,
                0M,
                NewDecimals(0, 0, 0, 0, 0, 34288M, 0, 0, 0, 0, 0, 0),
                0M,
                MidYearType.Opener,
                false
            };
            // Example 71
            //  1619
            //      Existing provider with new eligibility for a funding line - Variable profile - some default payment months in the past,
            //      others still in the future - eligibility for funding deemed to apply from the start of the funding year
            yield return new object[]
            {
                4,
                NewDecimals(6621M, 0, 6621M, 0, 0, 6621M, 0, 0, 6620.99M, 0, 0, 0),
                NewDecimals(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                26483.99M,
                0M,
                NewDecimals(0, 0, 0, 0, 13242M, 6621M, 0, 0, 6620.99M, 0, 0, 0),
                0M,
                MidYearType.Opener,
                false
            };
            // Example 72
            //  1619
            //      Existing provider with new eligibility for a funding line - Variable profile - some default payment months in the past,
            //      others still in the future - eligibility for funding deemed to apply from the point that provider became eligible
            yield return new object[]
            {
                4,
                NewDecimals(3862.25M, 0, 3862.25M, 0, 0, 3862.25M, 0, 0, 3862.24M, 0, 0, 0),
                NewDecimals(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                15448.99M,
                0M,
                NewDecimals(0, 0, 0, 0, 7724.50M, 3862.25M, 0, 0, 3862.24M, 0, 0, 0),
                0M,
                MidYearType.Opener,
                false
            };
            //  1619
            //      Existing provider with new eligibility for a funding line - Variable profile - some default payment months in the past,
            //      others still in the future - eligibility for funding deemed to apply from the point that provider became eligible
            yield return new object[]
            {
                4,
                NewDecimals(3862.25M, 0, 3862.25M, 0, 0, 3862.25M, 0, 0, 3862.24M, 0, 0, 0),
                NewDecimals(3862.25M, 0, 3862.25M, 0, 0, 3862.25M, 0, 0, 3862.24M, 0, 0, 0),
                15448.99M,
                15448.99M,
                NewDecimals(3862.25M, 0, 3862.25M, 0, 0, 3862.25M, 0, 0, 3862.24M, 0, 0, 0),
                0M,
                MidYearType.Opener,
                true
            };
            // Example 84 
            //  1619
            //      Mid year allocation decrease for provider - Variable profile - some default payment months in the past, 
            //      others still in the future
            yield return new object[]
            {
                6,
                NewDecimals(0, 0, 0, 2494.90M, 2494.87M, 2494.87M, 2494.87M, 2494.90M, 2494.87M, 2494.87M, 2494.87M, 2494.87M),
                NewDecimals(0, 0, 0, 2942.69M, 2942.65M, 2942.65M, 2942.65M, 2942.69M, 2942.65M, 2942.65M, 2942.65M, 2942.71M),
                22453.89M,
                26483.99M,
                NewDecimals(0, 0, 0, 2942.69M, 2942.65M, 2942.65M, 1151.52M, 2494.90M, 2494.87M, 2494.87M, 2494.87M, 2494.87M),
                0M,
                null,
                false
            };
            // Example 96 
            //  1619
            //      Existing provider is subject to de-merger. The existing provider remains open but a new provider is formed out of it leading to an allocation reduction
            //      – Single payment profile
            //      - default payment month in the past
            yield return new object[]
            {
                6,
                NewDecimals(0, 0, 0, 29876.37M, 0, 0, 0, 0, 0, 0, 0, 0),
                NewDecimals(0, 0, 0, 34288M, 0, 0, 0, 0, 0, 0, 0, 0),
                29876.37M,
                34288M,
                NewDecimals(0, 0, 0, 34288M, 0, 0, -4411.63M, 0, 0, 0, 0, 0),
                0M,
                null,
                false
            };
            // Example 99 
            //  1619
            //      Existing provider is subject to merger. The existing provider remains open and is expanded as a result of the consumption of one or more other providers
            //      – Single payment profile
            //      - default payment month in the past
            yield return new object[]
            {
                5,
                NewDecimals(0, 0, 0, 37867.25M, 0, 0, 0, 0, 0, 0, 0, 0),
                NewDecimals(0, 0, 0, 34288M, 0, 0, 0, 0, 0, 0, 0, 0),
                37867.25M,
                34288M,
                NewDecimals(0, 0, 0, 34288M, 0, 3579.25M, 0, 0, 0, 0, 0, 0),
                0M,
                null,
                false
            };
        }
    }
}
