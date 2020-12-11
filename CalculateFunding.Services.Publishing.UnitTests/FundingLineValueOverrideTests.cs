using System;
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
        public void UpdatesGeneratedResultIfPreviousVersionFundingLineIsPresent()
        {
            uint templateLineId = NewRandomUint();

            FundingLine generatedResultFundingLine = NewFundingLine(fl => fl
                .WithTemplateLineId(templateLineId)
                .WithFundingLineType(FundingLineType.Payment));

            GeneratedProviderResult generatedProviderResult = NewGeneratedProviderResult(_ =>
                _.WithFundlines(NewFundingLine(fl => fl
                    .WithTemplateLineId(templateLineId)
                    .WithFundingLineType(FundingLineType.Payment))));
            
            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ =>
                _.WithFundingLines(NewFundingLine(fl => fl.WithTemplateLineId(templateLineId)
                    .WithValue(98990M))));

            bool hasZeroedFundingLine = _fundingLineValueOverride.TryOverridePreviousFundingLineValues(publishedProviderVersion, 
                generatedProviderResult);

            hasZeroedFundingLine
                .Should()
                .BeTrue();

            generatedResultFundingLine.Value
                .Should()
                .BeNull();

        }

        [TestMethod]
        public void DoesNotUpdateGeneratedResultIfPreviousVersionHasNoMatchingFundingLine()
        {
            uint templateLineId = NewRandomUint();

            GeneratedProviderResult generatedProviderResult = NewGeneratedProviderResult(_ =>
                _.WithFundlines(NewFundingLine(fl => fl
                    .WithTemplateLineId(templateLineId)
                    .WithFundingLineType(FundingLineType.Payment))));

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ =>
                _.WithFundingLines(NewFundingLine()));

            bool hasZeroedFundingLine = _fundingLineValueOverride.TryOverridePreviousFundingLineValues(publishedProviderVersion, 
                generatedProviderResult);

            hasZeroedFundingLine
                .Should()
                .BeFalse();
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

        private static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);
        
            return publishedProviderVersionBuilder.Build();
        }
    }
}