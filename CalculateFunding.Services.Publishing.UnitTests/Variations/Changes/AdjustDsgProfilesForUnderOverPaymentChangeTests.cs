using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
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
            _fundingLineCode = NewRandomString();
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
                .WithOccurence(0)
                .WithTypeValue(MonthByVariationPointerIndex(variationPointerIndex))));

            await WhenTheChangeIsApplied();

            VariationContext
                .ErrorMessages
                .Should()
                .BeEmpty();
            
            AndTheFundingLinePeriodAmountsShouldBe(expectedAdjustedPeriodValues);
            AndTheFundingLineOverPaymentShouldBe(expectedRemainingOverPayment);
        }
        
        private static IEnumerable<object[]> OverAndUnderPaymentExamples()
        {
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
            IDictionary<string, decimal> overPayments = VariationContext.RefreshState.FundingLineOverPayments;
            
            if (expectedOverPayment == null)
            {
                overPayments
                    .Should()
                    .BeNull();
            }
            else
            {
                overPayments.TryGetValue(_fundingLineCode, out decimal actualOverPayment)
                    .Should()
                    .BeTrue();

                actualOverPayment
                    .Should()
                    .Be(expectedOverPayment);
            }
        }

        private ProfilePeriod[] AsProfilePeriods(params decimal[] periodValues)
        {
            return periodValues.Select((amount, index) => 
                NewProfilePeriod(_ => _.WithAmount(amount)
                .WithType(ProfilePeriodType.CalendarMonth)
                .WithOccurence(0)
                .WithTypeValue(MonthByVariationPointerIndex(index))
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

        private string MonthByVariationPointerIndex(int variationPointerIndex) => new DateTime(2020, variationPointerIndex + 1, 1)
            .ToString("MMMM");
    }
}