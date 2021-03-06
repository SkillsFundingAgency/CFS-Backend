using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class AdjustDsgProfilesForUnderOverPaymentChangeTests : VariationChangeTestBase
    {
        private int _year;
        private string _month;
        private string _fundingLineCode;

        [TestInitialize]
        public void SetUp()
        {
            Change = new AdjustDsgProfilesForUnderOverPaymentChange(VariationContext);    

            VariationContext.AllPublishedProviderSnapShots = new Dictionary<string, PublishedProviderSnapShots>
            {
                {VariationContext.ProviderId, new PublishedProviderSnapShots(VariationContext.PublishedProvider)}
            };
            
            _year = NewRandomYear();
            _month = NewRandomMonth();
            _fundingLineCode = NewRandomString();
        }

        [TestMethod]
        public async Task NoTotalAllocationChangeWithPreviousReleasedFundingDefect()
        {
            ProfilePeriod[] releasedProfilePeriods = GetProfilePeriods("released");
            ProfilePeriod[] newProfiledProfilePeriods = GetProfilePeriods("profiled");
            
            GivenTheFundingLines(NewFundingLine(_ => _.WithFundingLineCode(_fundingLineCode)
                .WithValue(89329262)
                .WithDistributionPeriods(NewDistributionPeriod(dp => 
                    dp.WithProfilePeriods(newProfiledProfilePeriods)))));
            AndThePreviousSnapShotFundingLines(NewFundingLine(_ => _.WithFundingLineCode(_fundingLineCode)
                .WithValue(89329262)
                .WithDistributionPeriods(NewDistributionPeriod(dp => 
                    dp.WithProfilePeriods(releasedProfilePeriods)))));
            AndTheVariationPointersForTheSpecification(NewVariationPointer(_ => _.WithFundingLineId(_fundingLineCode)
                .WithYear(2020)
                .WithOccurence(2)
                .WithTypeValue("November")));

            await WhenTheChangeIsApplied();

            VariationContext
                .ErrorMessages
                .Should()
                .BeEmpty();
            
            decimal adjustedTotal = newProfiledProfilePeriods.Sum(_ => _.ProfiledValue);

            adjustedTotal
                .Should()
                .Be(89329262);//total allocation should not be altered
        }

        [TestMethod]
        [DynamicData(nameof(OverAndUnderPaymentExamples), DynamicDataSourceType.Method)]
        public async Task BundlesUnderAndOverPaymentsAcrossTheFundingLinePeriodsAndCarriesOverRemainingOverPayments(int variationPointerIndex,
            decimal[] originalPeriodValues,
            decimal[] newTheoreticalPeriodValues,
            decimal totalAllocation,
            decimal previousTotalAllocation,
            decimal[] expectedAdjustedPeriodValues,
            decimal? expectedRemainingOverPayment)
        {
            GivenTheFundingLines(NewFundingLine(_ => _.WithFundingLineCode(_fundingLineCode)
                .WithValue(totalAllocation)
                .WithDistributionPeriods(NewDistributionPeriod(dp => 
                dp.WithProfilePeriods(AsProfilePeriods(newTheoreticalPeriodValues).ToArray())))));
            AndThePreviousSnapShotFundingLines(NewFundingLine(_ => _.WithFundingLineCode(_fundingLineCode)
                .WithValue(previousTotalAllocation)
                .WithDistributionPeriods(NewDistributionPeriod(dp => 
                    dp.WithProfilePeriods(AsProfilePeriods(originalPeriodValues).ToArray())))));
            AndTheVariationPointersForTheSpecification(NewVariationPointer(_ => _.WithFundingLineId(_fundingLineCode)
                .WithYear(_year)
                .WithOccurence(variationPointerIndex)
                .WithTypeValue(_month)));

            await WhenTheChangeIsApplied();

            VariationContext
                .ErrorMessages
                .Should()
                .BeEmpty();
            
            AndTheFundingLinePeriodAmountsShouldBe(expectedAdjustedPeriodValues);
            AndTheFundingLineOverPaymentShouldBe(expectedRemainingOverPayment);
        }

        private static ProfilePeriod[] GetProfilePeriods(string file)
            => typeof(AdjustDsgProfilesForUnderOverPaymentChangeTests)
                .Assembly
                .GetEmbeddedResourceFileContents($"CalculateFunding.Services.Publishing.UnitTests.Variations.Changes.{file}.json")
                .AsPoco<ProfilePeriod[]>();

        private static IEnumerable<object[]> OverAndUnderPaymentExamples()
        {
            //for defect 54331 - case with no change is incorrectly adjusting as an underpayment
            yield return new object []
            {
                3, 
                NewAmounts(1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000),
                NewAmounts(1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000),
                10000M,
                10000M,
                NewAmounts(1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000),
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
                (decimal?)null
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
                null,
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
                null,
            };
        }

        private static decimal[] NewAmounts(params decimal[] amounts) => amounts;

        private void AndTheFundingLinePeriodAmountsShouldBe(params decimal[] expectedAmounts)
        {
            FundingLine fundingLine = VariationContext.RefreshState.FundingLines.SingleOrDefault(_ => _.FundingLineCode == _fundingLineCode);

            fundingLine
                .Should()
                .NotBeNull();
            
            ProfilePeriod[] orderedProfilePeriods = new YearMonthOrderedProfilePeriods(fundingLine).ToArray();

            orderedProfilePeriods
                .Length
                .Should()
                .Be(expectedAmounts.Length);

            for (int amount = 0; amount < expectedAmounts.Length; amount++)
            {
                orderedProfilePeriods[amount]
                    .ProfiledValue
                    .Should()
                    .Be(expectedAmounts[amount], "Profiled value at index {0} should match expected value", amount);
            }
        }

        private void AndTheFundingLineOverPaymentShouldBe(decimal? expectedOverPayment)
        {
           IEnumerable<ProfilingCarryOver> overPayments = VariationContext.RefreshState.CarryOvers;
            
            if (expectedOverPayment == null)
            {
                overPayments
                    .Should()
                    .BeNull();
            }
            else
            {
                ProfilingCarryOver carryOver = overPayments.FirstOrDefault(_ => _.FundingLineCode == _fundingLineCode);

                carryOver
                    .Should()
                    .BeEquivalentTo(new ProfilingCarryOver
                    {
                        Type = ProfilingCarryOverType.DSGReProfiling,
                        Amount = expectedOverPayment.GetValueOrDefault(),
                        FundingLineCode = _fundingLineCode
                    });
            }
        }

        private ProfilePeriod[] AsProfilePeriods(params decimal[] periodValues)
        {
            return periodValues.Select((amount, index) => 
                NewProfilePeriod(_ => _.WithAmount(amount)
                .WithType(ProfilePeriodType.CalendarMonth)
                .WithOccurence(index)
                .WithTypeValue(_month)
                .WithYear(_year)))
                .ToArray();
        }

        private void AndThePreviousSnapShotFundingLines(params FundingLine[] fundingLines)
        {
            PublishedProvider previousSnapshot = VariationContext.GetPublishedProviderOriginalSnapShot(VariationContext.ProviderId);

            previousSnapshot.Current.FundingLines = fundingLines;
        }
        
        private int NewRandomYear() => NewRandomDateTime().Year;

        private static DateTime NewRandomDateTime() => new RandomDateTime();

        private static string NewRandomMonth() => NewRandomDateTime().ToString("MMMM");
    }
}