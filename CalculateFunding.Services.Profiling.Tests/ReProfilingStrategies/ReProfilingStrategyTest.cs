using System;
using System.Linq;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Profiling.Tests.ReProfilingStrategies
{
    public abstract class ReProfilingStrategyTest
    {
        private int _year;
        private string _month;
        
        protected ReProfileContext Context;
        protected ReProfileStrategyResult Result;
        protected IReProfilingStrategy ReProfiling;

        [TestInitialize]
        public void ReProfilingStrategyTestSetUp()
        {
            Context = new ReProfileContext();
            
            _year = NewRandomYear();
            _month = NewRandomMonth();
            
            Context = new ReProfileContext
            {
                Request = new ReProfileRequest(),
                ProfileResult = new AllocationProfileResponse()
            };
        }

        protected static decimal[] NewDecimals(params decimal[] amounts) => amounts;

        protected void AndTheFundingLinePeriodAmountsShouldBe(params decimal[] expectedAmounts)
        {
            DeliveryProfilePeriod[] orderedProfilePeriods = new YearMonthOrderedProfilePeriods<DeliveryProfilePeriod>(Result?.DeliveryProfilePeriods).ToArray();
            
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

        protected void ThenTheFundingLinePeriodAmountsShouldBe(params decimal[] expectedAmounts)
        {
            AndTheFundingLinePeriodAmountsShouldBe(expectedAmounts);
        }

        protected void AndTheCarryOverShouldBe(decimal? expectedOverPayment)
        {
            Result.CarryOverAmount
                .Should()
                .Be(expectedOverPayment.GetValueOrDefault());
        }

        protected ProfilePeriodPattern[] AsProfilePattern(params decimal[] profilePeriodPercentages)
        {
            return profilePeriodPercentages.Select((percentage,
                        index) =>
                    NewProfilePeriodPattern(_ => _.WithPercentage(percentage)
                        .WithType(PeriodType.CalendarMonth)
                        .WithYear(_year)
                        .WithPeriod(_month)
                        .WithOccurrence(index)
                        .Build()))
                .ToArray();
        }

        protected ExistingProfilePeriod[] AsExistingProfilePeriods(params decimal[] periodValues)
        {
            return periodValues.Select((amount, index) => 
                    NewExistingProfilePeriod(_ => _.WithProfiledValue(amount)
                        .WithPeriodType(PeriodType.CalendarMonth)
                        .WithOccurrence(index)
                        .WithTypeValue(_month)
                        .WithYear(_year)))
                .ToArray();
        }

        protected DeliveryProfilePeriod[] AsLatestProfiling(params decimal[] periodValues)
        {
            return periodValues.Select((amount, index) => 
                    NewDeliveryProfilePeriod(_ => _.WithProfiledValue(amount)
                        .WithPeriodType(PeriodType.CalendarMonth)
                        .WithOccurrence(index)
                        .WithTypeValue(_month)
                        .WithYear(_year)))
                .ToArray();
        }
        
        protected void GivenTheExistingProfilePeriods(params ExistingProfilePeriod[] existingProfilePeriods)
        {
            Context.Request.ExistingPeriods = existingProfilePeriods;
        }

        protected void AndTheExistingProfilePeriods(params ExistingProfilePeriod[] existingProfilePeriods)
        {
            GivenTheExistingProfilePeriods(existingProfilePeriods);
        }

        protected void AndThePreviousFundingTotal(decimal previousFundingTotal)
        {
            Context.Request.ExistingFundingLineTotal = previousFundingTotal;
        }

        protected void AndTheLatestFundingTotal(decimal latestFundingTotal)
        {
            Context.Request.FundingLineTotal = latestFundingTotal;
        }

        protected void AndTheVariationPointerIndex(int? variationPointerIndex)
        {
            Context.Request.VariationPointerIndex = variationPointerIndex;
        }

        protected void GivenTheLatestProfiling(params DeliveryProfilePeriod[] deliveryProfilePeriods)
        {
            Context.ProfileResult.DeliveryProfilePeriods = deliveryProfilePeriods;
        }

        protected void AndTheProfilePattern(FundingStreamPeriodProfilePattern profilePattern)
        {
            Context.ProfilePattern = profilePattern;
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

        protected FundingStreamPeriodProfilePattern NewFundingStreamPeriodProfilePattern(Action<FundingStreamPeriodProfilePatternBuilder> setUp = null)
        {
            FundingStreamPeriodProfilePatternBuilder fundingStreamPeriodProfilePatternBuilder = new FundingStreamPeriodProfilePatternBuilder();

            setUp?.Invoke(fundingStreamPeriodProfilePatternBuilder);
            
            return fundingStreamPeriodProfilePatternBuilder.Build();
        }

        private ProfilePeriodPattern NewProfilePeriodPattern(Action<ProfilePeriodPatternBuilder> setUp = null)
        {
            ProfilePeriodPatternBuilder profilePeriodPatternBuilder = new ProfilePeriodPatternBuilder();
            
            setUp?.Invoke(profilePeriodPatternBuilder);

            return profilePeriodPatternBuilder.Build();
        }

        private int NewRandomYear() => NewRandomDateTime().Year;
        
        private static DateTime NewRandomDateTime() => new RandomDateTime();
        
        private static string NewRandomMonth() => NewRandomDateTime().ToString("MMMM");

        protected void WhenTheFundingLineIsReProfiled()
            => Result = ReProfiling.ReProfile(Context);
    }
}