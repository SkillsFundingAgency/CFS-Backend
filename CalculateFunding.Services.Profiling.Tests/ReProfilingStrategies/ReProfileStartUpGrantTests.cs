using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Profiling.Tests.ReProfilingStrategies
{
    [TestClass]
    public class ReProfileStartUpGrantTests : ReProfilingStrategyTest
    {
        [TestInitialize]
        public void SetUp()
        {
            ReProfiling = new ReProfileStartUpGrant();
        }

        [TestMethod]
        [DynamicData(nameof(StartUpGrantExamples), DynamicDataSourceType.Method)]
        public void DistributesStartUpGrantsOverFirstAvailablePeriodsSubsequentToOpening(int variationPointerIndex,
            decimal[] newTheoreticalPeriodValues,
            decimal totalAllocation,
            decimal[] expectedAdjustedPeriodValues)
        {
            GivenTheLatestProfiling(AsLatestProfiling(newTheoreticalPeriodValues));
            AndTheVariationPointerIndex(variationPointerIndex);
            AndTheLatestFundingTotal(totalAllocation);
            
            WhenTheFundingLineIsReProfiled();
            
            AndTheFundingLinePeriodAmountsShouldBe(expectedAdjustedPeriodValues);
            AndTheCarryOverShouldBe(0);
        }
        
        private static IEnumerable<object[]> StartUpGrantExamples()
        {
            //Example 1 - New provider eligible for SUG Part A opens at the start of the funding year
            yield return new object []
            {
                0, 
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                56821882M,
                NewAmounts(28410941M, 14205470.5M, 14205470.5M, 0, 0, 0, 0, 0, 0, 0, 0, 0)
            };
            //Example 2 - New provider eligible for SUG Part A opens at the start of the funding year
            yield return new object []
            {
                0, 
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                28761972M,
                NewAmounts(14380986M, 7190493M, 7190493M, 0, 0, 0, 0, 0, 0, 0, 0, 0)
            };
            //Example 3 - New provider eligible for SUG Part A opens part way through the funding year (at least 3 months remaining)
            yield return new object []
            {
                5, 
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                34617261M,
                NewAmounts(0, 0, 0, 0, 0, 17308630.5M, 8654315.25M, 8654315.25M, 0, 0, 0, 0)
            };
            //Example 4 - New provider eligible for SUG Part A opens part way through the funding year (at least 3 months remaining)
            yield return new object []
            {
                3, 
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                76152317M,
                NewAmounts(0, 0, 0, 38076158.5M, 19038079.25M, 19038079.25M, 0, 0, 0, 0, 0, 0)
            };
            //Example 5 - New provider eligible for SUG Part A opens part way through the funding year (3 months remaining)
            yield return new object []
            {
                9, 
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                54552817M,
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 27276408.5M, 13638204.25M, 13638204.25M)
            };
            //Example 6 - New provider eligible for SUG Part A opens part way through the funding year (3 months remaining)
            yield return new object []
            {
                9, 
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                54777182M,
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 27388591M, 13694295.5M, 13694295.5M)
            };
            //Example 7 - New provider eligible for SUG Part A opens part way through the funding year (2 months remaining)
            yield return new object []
            {
                10, 
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                27612335M,
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 13806167.5M, 13806167.5M)
            };
            //Example 8 - New provider eligible for SUG Part A opens part way through the funding year (2 months remaining)
            yield return new object []
            {
                10, 
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                28715328M,
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 14357664M, 14357664M)
            };
            //Example 9 - New provider eligible for SUG Part A opens part way through the funding year (1 month remaining)
            yield return new object []
            {
                11, 
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                57341928M,
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 57341928M)
            };
            //Example 10 - New provider eligible for SUG Part A opens part way through the funding year (1 month remaining)
            yield return new object []
            {
                11, 
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                76231652M,
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 76231652M)
            };
            //rounding example 1 - to 2 dp (to zero) carry remainders to final payment
            // carry .005 + 0.0075 
            yield return new object []
            {
                9, 
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                60.75M,
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 30.37M, 15.18M, 15.2M)
            };
            //rounding example 2 - to 2 dp (to zero) carry remainders to final payment
            // carry .005
            yield return new object []
            {
                10, 
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                60.75M,
                NewAmounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 30.37M, 30.38M)
            };
        }
    }
}