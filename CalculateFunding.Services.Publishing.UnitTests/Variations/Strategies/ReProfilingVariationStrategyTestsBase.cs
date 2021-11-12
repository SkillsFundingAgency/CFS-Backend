using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Changes;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    public abstract class ReProfilingVariationStrategyTestsBase : ProfilingVariationStrategyTestBase
    {
        [TestMethod]
        public async Task FailsPreconditionCheckIfThereAreNoVariationPointers()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = NewRandomString());
            AndTheRefreshStateFundingLines(NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                .WithFundingLineType(FundingLineType.Payment)
                .WithValue(NewRandomNumber())
                .WithDistributionPeriods(NewDistributionPeriod(dp =>
                    dp.WithProfilePeriods(NewProfilePeriod())))));

            await WhenTheVariationsAreProcessed();

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }

        protected void AndTheVariationPointers(params ProfileVariationPointer[] variationPointers)
        {
            VariationContext.VariationPointers = variationPointers;
        }

        protected void AndTheAffectedFundingLinesAreNotTracked(params string[] affectedFundingLines)
            => VariationContext.AffectedFundingLineCodes?
                .Should()
                .NotContain(affectedFundingLines);

        protected void AndTheAffectedFundingLinesWereTracked(params string[] affectedFundingLines)
            => VariationContext.AffectedFundingLineCodes
                .Should()
                .BeEquivalentTo(affectedFundingLines);

        protected ProfileVariationPointer NewVariationPointer(Action<ProfileVariationPointerBuilder> setUp = null)
        {
            ProfileVariationPointerBuilder variationPointerBuilder = new ProfileVariationPointerBuilder();

            setUp?.Invoke(variationPointerBuilder);

            return variationPointerBuilder.Build();
        }

        protected string NewRandomMonth() => ((DateTime) new RandomDateTime()).ToString("MMMM");

        protected void AndThePriorStateFundingLines(params FundingLine[] fundingLines)
            => VariationContext.PriorState.FundingLines = fundingLines;
    }
}