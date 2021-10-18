using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Profiling.Tests.ReProfilingStrategies
{
    [TestClass]
    public class ReProfileRemainingFundingForPeriodTests : ReProfilingStrategyTest
    {
        [TestInitialize]
        public void SetUp()
        {
            ReProfiling = new ReProfileRemainingFundingForPeriod();
        }

        [TestMethod]
        [DynamicData(nameof(RemainingFundingExamples), DynamicDataSourceType.Method)]
        public void BundlesUnderAndOverPaymentsAcrossTheFundingLinePeriodsAndCarriesOverRemainingOverPayments(int variationPointerIndex,
            decimal[] originalPeriodValues,
            decimal[] newTheoreticalPeriodValues,
            decimal[] expectedAdjustedPeriodValues)
        {
            GivenTheLatestProfiling(AsLatestProfiling(newTheoreticalPeriodValues));
            AndTheExistingProfilePeriods(AsExistingProfilePeriods(originalPeriodValues.Take(variationPointerIndex).ToArray()));
            AndTheLatestFundingTotal(newTheoreticalPeriodValues.Sum());

            WhenTheFundingLineIsReProfiled();
            
            AndTheFundingLinePeriodAmountsShouldBe(expectedAdjustedPeriodValues);
            AndTheCarryOverShouldBe(0);  
        }
        
        private static IEnumerable<object[]> RemainingFundingExamples()
        {
            yield return new object []
            {
                3, 
                NewDecimals(1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000),
                NewDecimals(1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100),
                NewDecimals(0, 0, 0, 1571.43M, 1571.43M, 1571.43M, 1571.43M, 1571.43M, 1571.43M, 1571.42M),
            };
            yield return new object []
            {
                6, 
                NewDecimals(1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100),
                NewDecimals(950, 950, 950, 950, 950, 950, 950, 950, 950, 950),
                NewDecimals(0, 0, 0, 0, 0, 0, 2375, 2375, 2375, 2375),
            };
            yield return new object []
            {
                9, 
                NewDecimals(1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000),
                NewDecimals(800, 800, 800, 800, 800, 800, 800, 800, 800, 800),
                NewDecimals(0, 0, 0, 0, 0, 0, 0, 0, 0, 8000),
            };
            yield return new object[]
            {
                3,
                NewDecimals(0, 0, 1000, 0, 0, 1000, 0, 0, 0, 1000),
                NewDecimals(0, 0, 800, 0, 0, 800, 0, 0, 0, 800),
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
                NewDecimals(0, 0, 0, 0, 0, 0, 0, 0, 13958.85M, 13958.84M, 0, 0),
            };
        }
    }
}