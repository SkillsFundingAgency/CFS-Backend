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
            decimal totalAllocation,
            decimal previousTotalAllocation,
            decimal[] expectedAdjustedPeriodValues,
            decimal? expectedRemainingOverPayment)
        {
            //just need something in the refresh lines though we throw it all away during the re profiling
            GivenTheLatestProfiling(AsLatestProfiling(originalPeriodValues));
            GivenTheExistingProfilePeriods(AsExistingProfilePeriods(originalPeriodValues.Take(variationPointerIndex).ToArray()));
            
            AndThePreviousFundingTotal(previousTotalAllocation);
            AndTheLatestFundingTotal(totalAllocation);
            
            WhenTheFundingLineIsReProfiled();
            
            AndTheFundingLinePeriodAmountsShouldBe(expectedAdjustedPeriodValues);
            AndTheCarryOverShouldBe(expectedRemainingOverPayment);
        }

        private static IEnumerable<object[]> OverAndUnderPaymentExamples()
        {
            //Example 2 - Provider overpaid - recovery of overpayment spread over remaining instalments
            yield return new object[]
            {
                5,
                NewAmounts(2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000),
                20500M,
                24000M,
                NewAmounts(2000, 2000, 2000, 2000, 2000, 1500, 1500, 1500, 1500, 1500, 1500, 1500),
                null
            };
            //Example 3 - Provider overpaid - recovery of overpayment spread over remaining instalments - negated in funding period without reduction to zero
            yield return new object[]
            {
                8,
                NewAmounts(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000),
                33600M,
                36000M,
                NewAmounts(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 2400, 2400, 2400, 2400),
                null
            };
            //Example 4 - Provider overpaid - recovery of overpayment spread over remaining instalments - negated in funding period through reduction to zero
            yield return new object[]
            {
                8,
                NewAmounts(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000),
                24000M,
                36000M,
                NewAmounts(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 0, 0, 0, 0),
                null
            };
            //Example 5 - Provider overpaid - recovery of overpayment spread over remaining instalments with balance to carry forward
            yield return new object[]
            {
                8,
                NewAmounts(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000),
                22000M,
                36000M,
                NewAmounts(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 0, 0, 0, 0),
                2000M
            };
        }
    }
}