using System;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class FundingLineValueOverrideTests
    {
        private FundingLineValueOverride _fundingLineValueOverride;

        [TestInitialize]
        public void SetUp()
        {
            _fundingLineValueOverride = new FundingLineValueOverride();
        }

        [TestMethod]
        public void WhenProviderHasNoPreviousFundingThenHasPreviousFundingReturnsFalse()
        {
            uint templateLineId = NewRandomUint();

            GeneratedProviderResult generatedProviderResult = NewGeneratedProviderResult(_ =>
                _.WithFundlines(NewFundingLine(fl => fl
                    .WithTemplateLineId(templateLineId)
                    .WithFundingLineType(FundingLineType.Payment))));

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion();

            bool hasPreviousFunding = _fundingLineValueOverride.HasPreviousFunding(generatedProviderResult, publishedProviderVersion);

            hasPreviousFunding
                .Should()
                .Be(false);
        }

        [TestMethod]
        public void WhenProviderHasPreviousFundingThenHasPreviousFundingReturnsTrue()
        {
            uint templateLineId = NewRandomUint();

            GeneratedProviderResult generatedProviderResult = NewGeneratedProviderResult(_ =>
                _.WithFundlines(NewFundingLine(fl => fl
                    .WithTemplateLineId(templateLineId)
                    .WithFundingLineType(FundingLineType.Payment))));

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ =>
                _.WithFundingLines(NewFundingLine(fl => fl.WithTemplateLineId(templateLineId)
                    .WithValue(98990M))));

            bool hasPreviousFunding = _fundingLineValueOverride.HasPreviousFunding(generatedProviderResult, publishedProviderVersion);

            hasPreviousFunding
                .Should()
                .Be(true);
        }

        [TestMethod]
        public void UpdatesGeneratedResultIfPreviousVersionFundingLineIsPresent()
        {
            uint templateLineId = NewRandomUint();

            GeneratedProviderResult generatedProviderResult = NewGeneratedProviderResult(_ =>
                _.WithFundlines(NewFundingLine(fl => fl
                    .WithTemplateLineId(templateLineId)
                    .WithFundingLineType(FundingLineType.Payment))));

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(_ =>
                _.WithFundingLines(NewFundingLine(fl => fl.WithTemplateLineId(templateLineId)
                    .WithValue(98990M))))));

            _fundingLineValueOverride.OverridePreviousFundingLineValues(publishedProvider, 
                generatedProviderResult);

            generatedProviderResult.FundingLines
                .Where(_ => _.TemplateLineId == templateLineId)
                .Single()
                .Value
                .Should()
                .Be(null);
        }

        [TestMethod]
        public void UpdatesGeneratedResultIfPreviousVersionFundingLineIsPresentButMultipleFundingLinesHaveAValue()
        {
            uint templateLineId = NewRandomUint();
            uint templateLineId2 = NewRandomUint();

            GeneratedProviderResult generatedProviderResult = NewGeneratedProviderResult(_ =>
                _.WithFundlines(NewFundingLine(fl => fl
                    .WithTemplateLineId(templateLineId)
                    .WithFundingLineType(FundingLineType.Payment)
                    .WithValue(98990M)),
                    NewFundingLine(fl => fl
                    .WithTemplateLineId(templateLineId2)
                    .WithFundingLineType(FundingLineType.Payment))));

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(_ =>
                _.WithFundingLines(NewFundingLine(fl => fl.WithTemplateLineId(templateLineId)
                    .WithValue(9000M)),
                    NewFundingLine(fl => fl.WithTemplateLineId(templateLineId2)
                    .WithValue(2000M))))));

            _fundingLineValueOverride.OverridePreviousFundingLineValues(publishedProvider,
                generatedProviderResult);

            generatedProviderResult.FundingLines
                .Where(_ => _.TemplateLineId == templateLineId)
                .Single()
                .Value
                .Should()
                .Be(98990M);

            generatedProviderResult.FundingLines
                .Where(_ => _.TemplateLineId == templateLineId2)
                .Single()
                .Value
                .Should()
                .Be(null);
        }

        [TestMethod]
        public void DoesNotUpdateGeneratedResultIfPreviousVersionHasNoMatchingFundingLine()
        {
            uint templateLineId = NewRandomUint();

            GeneratedProviderResult generatedProviderResult = NewGeneratedProviderResult(_ =>
                _.WithFundlines(NewFundingLine(fl => fl
                    .WithTemplateLineId(templateLineId)
                    .WithFundingLineType(FundingLineType.Payment))));

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(_ =>
                _.WithFundingLines(NewFundingLine()))));

            _fundingLineValueOverride.OverridePreviousFundingLineValues(publishedProvider, 
                generatedProviderResult);

            generatedProviderResult.FundingLines
                .Where(_ => _.TemplateLineId == templateLineId)
                .Single()
                .Value
                .Should()
                .Be(null);
        }

        private uint NewRandomUint() => (uint) new RandomNumberBetween(1, int.MaxValue);

        private static GeneratedProviderResult NewGeneratedProviderResult(Action<GeneratedProviderResultBuilder> setUp = null)
        {
            GeneratedProviderResultBuilder generatedProviderResultBuilder = new GeneratedProviderResultBuilder();

            setUp?.Invoke(generatedProviderResultBuilder);
        
            return generatedProviderResultBuilder.Build();
        }

        private static FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);
        
            return fundingLineBuilder.Build();
        }

        private static PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);
        
            return publishedProviderVersionBuilder.Build();
        }
    }
}