using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class ReProfilingVariationStrategyTests : ProfilingVariationStrategyTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            VariationStrategy = new ReProfilingVariationStrategy();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfThereAreNoVariationPointers()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = NewRandomString());
            AndTheRefreshStateFundingLines(NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                .WithFundingLineType(FundingLineType.Payment)
                .WithValue(NewRandomNumber())
                .WithDistributionPeriods(NewDistributionPeriod(dp =>
                    dp.WithProfilePeriods(NewProfilePeriod())))));

            await WhenTheVariationsAreDetermined();

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }
        
        [TestMethod]
        public async Task FailsPreconditionCheckIfThereAreNoPaidPeriods()
        {
           int year = NewRandomNumber();
            string month = NewRandomMonth();

            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = NewRandomString());
            AndTheRefreshStateFundingLines(NewFundingLine(),
                NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                    .WithFundingLineType(FundingLineType.Payment)
                    .WithValue(NewRandomNumber())
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(0)),
                            NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(1)))))));
            AndThePriorStateFundingLines(NewFundingLine(),
                NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                    .WithFundingLineType(FundingLineType.Payment)
                    .WithValue(NewRandomNumber())
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(0)),
                            NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(1)))))));
            AndTheVariationPointers(NewVariationPointer(_ => _.WithYear(year)
                .WithTypeValue(month)
                .WithOccurence(0)
                .WithFundingLineId(FundingLineCode)
                .WithPeriodType(ProfilePeriodType.CalendarMonth.ToString())));

            await WhenTheVariationsAreDetermined();


            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task TracksAffectedFundingLineCodesAndQueuesReProfileVariationChangeIfProfilingDiffers()
        {
            int year = NewRandomNumber();
            string month = NewRandomMonth();

            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = NewRandomString());
            AndTheRefreshStateFundingLines(NewFundingLine(),
                NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                    .WithFundingLineType(FundingLineType.Payment)
                    .WithValue(NewRandomNumber())
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(0)),
                            NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(1)))))));
            AndThePriorStateFundingLines(NewFundingLine(),
                NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                    .WithFundingLineType(FundingLineType.Payment)
                    .WithValue(NewRandomNumber())
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(0)),
                            NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(1)))))));
            AndTheVariationPointers(NewVariationPointer(_ => _.WithYear(year)
                .WithTypeValue(month)
                .WithOccurence(1)
                .WithFundingLineId(FundingLineCode)
                .WithPeriodType(ProfilePeriodType.CalendarMonth.ToString())));

            await WhenTheVariationsAreDetermined();

            ThenTheVariationChangeWasQueued<ReProfileVariationChange>();
            AndTheAffectedFundingLinesWereTracked(FundingLineCode);
        }

        private void AndTheVariationPointers(params ProfileVariationPointer[] variationPointers)
        {
            VariationContext.VariationPointers = variationPointers;
        }

        private void AndTheAffectedFundingLinesWereTracked(params string[] affectedFundingLines)
            => VariationContext.AffectedFundingLineCodes
                .Should()
                .BeEquivalentTo(affectedFundingLines);

        private ProfileVariationPointer NewVariationPointer(Action<ProfileVariationPointerBuilder> setUp = null)
        {
            ProfileVariationPointerBuilder variationPointerBuilder = new ProfileVariationPointerBuilder();

            setUp?.Invoke(variationPointerBuilder);

            return variationPointerBuilder.Build();
        }

        private void AndThePriorStateFundingLines(params FundingLine[] fundingLines)
            => VariationContext.PriorState.FundingLines = fundingLines;

        private string NewRandomMonth() => ((DateTime) new RandomDateTime()).ToString("MMMM");
    }
}