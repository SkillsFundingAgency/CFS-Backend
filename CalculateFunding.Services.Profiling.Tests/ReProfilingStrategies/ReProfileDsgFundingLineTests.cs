using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Profiling.Tests.ReProfilingStrategies
{
    [TestClass]
    public class ReProfileDsgFundingLineTests : ReProfilingStrategyTest
    {
        [TestInitialize]
        public void SetUp()
        {
            ReProfiling = new ReProfileDsgFundingLine();
        }
        
        [TestMethod]
        public void NoTotalAllocationChangeWithPreviousReleasedFundingDefect()
        {
            ExistingProfilePeriod[] releasedProfilePeriods = GetProfilePeriods<ExistingProfilePeriod>("existing");
            DeliveryProfilePeriod[] newProfiledProfilePeriods = GetProfilePeriods<DeliveryProfilePeriod>("delivery");

            GivenTheLatestProfiling(newProfiledProfilePeriods);
            AndTheExistingProfilePeriods(releasedProfilePeriods);

            WhenTheFundingLineIsReProfiled();

            decimal adjustedTotal = newProfiledProfilePeriods.Sum(_ => _.GetProfileValue());

            adjustedTotal
                .Should()
                .Be(57170720M);//total allocation should not be altered
        }

        [TestMethod]
        [DynamicData(nameof(OverAndUnderPaymentExamples), DynamicDataSourceType.Method)]
        public void BundlesUnderAndOverPaymentsAcrossTheFundingLinePeriodsAndCarriesOverRemainingOverPayments(int variationPointerIndex,
            decimal[] originalPeriodValues,
            decimal[] newTheoreticalPeriodValues,
            decimal totalAllocation,
            decimal previousTotalAllocation,
            decimal[] expectedAdjustedPeriodValues,
            decimal? expectedRemainingOverPayment)
        {
            GivenTheLatestProfiling(AsLatestProfiling(newTheoreticalPeriodValues));
            AndTheExistingProfilePeriods(AsExistingProfilePeriods(originalPeriodValues.Take(variationPointerIndex).ToArray()));
            AndThePreviousFundingTotal(previousTotalAllocation);
            AndTheLatestFundingTotal(totalAllocation);
            
            WhenTheFundingLineIsReProfiled();
            
            AndTheFundingLinePeriodAmountsShouldBe(expectedAdjustedPeriodValues);
            AndTheCarryOverShouldBe(expectedRemainingOverPayment);
        }

        private static T[] GetProfilePeriods<T>(string file)
            => typeof(ReProfileDsgFundingLineTests)
                .Assembly
                .GetEmbeddedResourceFileContents($"CalculateFunding.Services.Profiling.Tests.Resources.{file}.json")
                .AsPoco<T[]>();

        private static IEnumerable<object[]> OverAndUnderPaymentExamples()
        {
            //for defect 54331 - case with no change is incorrectly adjusting as an underpayment
            yield return new object []
            {
                16, 
                NewAmounts(3760098,3760098,3760098,3760098,3760098,3760098,3760098,3760098,2279346,3595570,3595570,3595570,3595570,3595570,3595570,3595570,3214770,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573182),
                NewAmounts(3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573182),
                89329262M,
                89329262M,
                NewAmounts(3760098,3760098,3760098,3760098,3760098,3760098,3760098,3760098,2279346,3595570,3595570,3595570,3595570,3595570,3595570,3595570,3214770,3573170,3573170,3573170,3573170,3573170,3573170,3573170,3573182),
                (decimal?)null
            };
            yield return new object []
            {
                3, 
                NewAmounts(1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000),
                NewAmounts(1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100, 1100),
                11000M,
                10000M,
                NewAmounts(1000, 1000, 1000, 1400, 1100, 1100, 1100, 1100, 1100, 1100),
                null
            };
            yield return new object []
            {
                6, 
                NewAmounts(1000, 1000, 1000, 1400, 1100, 1100, 1100, 1100, 1100, 1100),
                NewAmounts(950, 950, 950, 950, 950, 950, 950, 950, 950, 950),
                9500M,
                11000M,
                NewAmounts(1000, 1000, 1000, 1400, 1100, 1100, 50, 950, 950, 950),
                null
            };
            yield return new object []
            {
                9, 
                NewAmounts(1000, 1000, 1000, 1400, 1100, 1100, 50, 950, 950, 950),
                NewAmounts(800, 800, 800, 800, 800, 800, 800, 800, 800, 800),
                8000M,
                9500M,
                NewAmounts(1000, 1000, 1000, 1400, 1100, 1100, 50, 950, 950, 0),
                550M
            };
            //example from robs xls
            yield return new object []
            {
                7, 
                NewAmounts(9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074,9448074, 9448074, 9448074, 94480741),
                NewAmounts(9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661,9222661, 9222661, 9222661, 9222675),
                230566539M,
                236566539M,
                NewAmounts(9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 7644770, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661,9222661, 9222661, 9222661, 9222675),
                null
            };
            //same as Robs example but now the variation pointer puts us on the final profile period  
            yield return new object []
            {
                24, 
                NewAmounts(9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074,9448074, 9448074, 9448074, 94480741),
                NewAmounts(9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661, 9222661,9222661, 9222661, 9222661, 9222675),
                230566539M,
                236566539M,
                NewAmounts(9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074, 9448074,9448074, 9448074, 9448074, 3812763),
                null
            };
        }
    }
}