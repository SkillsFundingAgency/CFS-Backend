using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Profiling.Tests.ReProfilingStrategies
{
    [TestClass]
    public class ReProfileDsgFundingLineTests
    {
        private int _year;
        private string _month;
        private ReProfileContext _context;

        private ReProfileDsgFundingLine _reProfiling;

        private ReProfileStrategyResult _result;

        [TestInitialize]
        public void SetUp()
        {
            _context = new ReProfileContext();
            
            _year = NewRandomYear();
            _month = NewRandomMonth();
            
            _context = new ReProfileContext
            {
                Request = new ReProfileRequest(),
                ProfileResult = new AllocationProfileResponse()
            };
            
            _reProfiling = new ReProfileDsgFundingLine();
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

        private void WhenTheFundingLineIsReProfiled()
            => _result = _reProfiling.ReProfile(_context);

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


        private static decimal[] NewAmounts(params decimal[] amounts) => amounts;

        private void AndTheFundingLinePeriodAmountsShouldBe(params decimal[] expectedAmounts)
        {
            DeliveryProfilePeriod[] orderedProfilePeriods = new YearMonthOrderedProfilePeriods<DeliveryProfilePeriod>(_result?.DeliveryProfilePeriods).ToArray();
            
            orderedProfilePeriods
                .Length
                .Should()
                .Be(expectedAmounts.Length);
            
            for (int amount = 0; amount < expectedAmounts.Length; amount++)
            {
                orderedProfilePeriods[amount]
                    .GetProfileValue()
                    .Should()
                    .Be(expectedAmounts[amount], "Profiled value at index {0} should match expected value", amount);
            }
        }

        private void AndTheCarryOverShouldBe(decimal? expectedOverPayment)
        {
            _result.CarryOverAmount
                .Should()
                .Be(expectedOverPayment.GetValueOrDefault());
        }

        private ExistingProfilePeriod[] AsExistingProfilePeriods(params decimal[] periodValues)
        {
            return periodValues.Select((amount, index) => 
                    NewExistingProfilePeriod(_ => _.WithProfiledValue(amount)
                .WithPeriodType(PeriodType.CalendarMonth)
                .WithOccurrence(index)
                .WithTypeValue(_month)
                .WithYear(_year)))
                .ToArray();
        }
        
        private DeliveryProfilePeriod[] AsLatestProfiling(params decimal[] periodValues)
        {
            return periodValues.Select((amount, index) => 
                    NewDeliveryProfilePeriod(_ => _.WithProfiledValue(amount)
                        .WithPeriodType(PeriodType.CalendarMonth)
                        .WithOccurrence(index)
                        .WithTypeValue(_month)
                        .WithYear(_year)))
                .ToArray();
        }

        private void AndTheExistingProfilePeriods(params ExistingProfilePeriod[] existingProfilePeriods)
        {
            _context.Request.ExistingPeriods = existingProfilePeriods;
        }

        private void AndThePreviousFundingTotal(decimal previousFundingTotal)
        {
            _context.Request.ExistingFundingLineTotal = previousFundingTotal;
        }
        
        private void AndTheLatestFundingTotal(decimal latestFundingTotal)
        {
            _context.Request.FundingLineTotal = latestFundingTotal;
        }

        private void GivenTheLatestProfiling(params DeliveryProfilePeriod[] deliveryProfilePeriods)
        {
            _context.ProfileResult.DeliveryProfilePeriods = deliveryProfilePeriods;
        }

        private ExistingProfilePeriod NewExistingProfilePeriod(Action<ExistingProfilePeriodBuilder> setUp = null)
        {
            ExistingProfilePeriodBuilder existingProfilePeriodBuilder = new ExistingProfilePeriodBuilder();

            setUp?.Invoke(existingProfilePeriodBuilder);
            
            return existingProfilePeriodBuilder.Build();
        }

        private DeliveryProfilePeriod NewDeliveryProfilePeriod(Action<DeliveryProfilePeriodBuilder> setUp = null)
        {
            DeliveryProfilePeriodBuilder deliveryProfilePeriodBuilder = new DeliveryProfilePeriodBuilder();

            setUp?.Invoke(deliveryProfilePeriodBuilder);
            
            return deliveryProfilePeriodBuilder.Build();
        }

        private int NewRandomYear() => NewRandomDateTime().Year;

        private static DateTime NewRandomDateTime() => new RandomDateTime();

        private static string NewRandomMonth() => NewRandomDateTime().ToString("MMMM");   
    }
}