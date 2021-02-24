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
            
            WhenTheFundingLineIsReProfiled();
            
            AndTheFundingLinePeriodAmountsShouldBe(expectedAdjustedPeriodValues);
            AndTheCarryOverShouldBe(0);  
        }
        
        private static IEnumerable<object[]> RemainingFundingExamples()
        {
            yield return new object []
            {
                3, 
                NewAmounts(1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000),
                NewAmounts(1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100),
                NewAmounts(0, 0, 0, 1100, 1100, 1100, 1100, 1100, 1100, 1100),
            };
            yield return new object []
            {
                6, 
                NewAmounts(1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100),
                NewAmounts(950, 950, 950, 950, 950, 950, 950, 950, 950, 950),
                NewAmounts(0, 0, 0, 0, 0, 0, 950, 950, 950, 950),
            };
            yield return new object []
            {
                9, 
                NewAmounts(1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000),
                NewAmounts(800, 800, 800, 800, 800, 800, 800, 800, 800, 800),
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 800),
            };
        }
    }
}