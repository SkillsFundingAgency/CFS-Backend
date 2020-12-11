using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedProviderExclusionCheckTests
    {
        [TestMethod]
        public void ShouldExclueProviderIfAllPaymentFundingLinesHaveNoValues()
        {
            string providerId = NewRandomString();
            uint templateLineId1 = NewRandomUint();
            uint templateLineId2 = NewRandomUint();

            GeneratedProviderResult generatedProviderResult = NewGeneratedProviderResult(_ =>
            _.WithFundlines(NewFundingLine(f => f.WithFundingLineType(FundingLineType.Payment).WithTemplateLineId(templateLineId1)),
                            NewFundingLine(f => f.WithFundingLineType(FundingLineType.Payment).WithTemplateLineId(templateLineId2)))
            .WithProvider(NewProvider(p => p.WithProviderId(providerId))));

            TemplateFundingLine[] templateFundingLines = new[] 
            {
                NewTemplateFundingLine(_ => _.WithTemplateLineId(templateLineId1)),
                NewTemplateFundingLine(_ => _.WithTemplateLineId(templateLineId2))
            };

            PublishedProviderExclusionCheck exclusionCheck = new PublishedProviderExclusionCheck();
            PublishedProviderExclusionCheckResult result = exclusionCheck.ShouldBeExcluded(generatedProviderResult, templateFundingLines);

            result.ProviderId
                .Should()
                .Be(providerId);

            result.ShouldBeExcluded
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void ShouldExclueProviderIfNoPaymentFundingLines()
        {
            string providerId = NewRandomString();
            uint templateLineId1 = NewRandomUint();
            uint templateLineId2 = NewRandomUint();

            GeneratedProviderResult generatedProviderResult = NewGeneratedProviderResult(_ =>
            _.WithFundlines(NewFundingLine(f => f.WithFundingLineType(FundingLineType.Information).WithTemplateLineId(templateLineId1)),
                            NewFundingLine(f => f.WithFundingLineType(FundingLineType.Information).WithTemplateLineId(templateLineId2)))
            .WithProvider(NewProvider(p => p.WithProviderId(providerId))));

            TemplateFundingLine[] templateFundingLines = new[]
            {
                NewTemplateFundingLine(_ => _.WithTemplateLineId(templateLineId1)),
                NewTemplateFundingLine(_ => _.WithTemplateLineId(templateLineId2))
            };

            PublishedProviderExclusionCheck exclusionCheck = new PublishedProviderExclusionCheck();
            PublishedProviderExclusionCheckResult result = exclusionCheck.ShouldBeExcluded(generatedProviderResult, templateFundingLines);

            result.ProviderId
                .Should()
                .Be(providerId);

            result.ShouldBeExcluded
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void ShouldNotExclueProviderIfAtleastOnePaymentFundingLineHaveValue()
        {
            string providerId = NewRandomString();
            uint templateLineId1 = NewRandomUint();
            uint templateLineId2 = NewRandomUint();

            GeneratedProviderResult generatedProviderResult = NewGeneratedProviderResult(_ =>
            _.WithFundlines(NewFundingLine(f => f.WithFundingLineType(FundingLineType.Payment).WithTemplateLineId(templateLineId1)),
                            NewFundingLine(f => f.WithFundingLineType(FundingLineType.Payment).WithTemplateLineId(templateLineId2).WithValue(1m)))
            .WithProvider(NewProvider(p => p.WithProviderId(providerId))));

            TemplateFundingLine[] templateFundingLines = new[]
            {
                NewTemplateFundingLine(_ => _.WithTemplateLineId(templateLineId1)),
                NewTemplateFundingLine(_ => _.WithTemplateLineId(templateLineId2))
            };

            PublishedProviderExclusionCheck exclusionCheck = new PublishedProviderExclusionCheck();
            PublishedProviderExclusionCheckResult result = exclusionCheck.ShouldBeExcluded(generatedProviderResult, templateFundingLines);

            result.ProviderId
                .Should()
                .Be(providerId);

            result.ShouldBeExcluded
                .Should()
                .BeFalse();
        }

        private static string NewRandomString() => new RandomString();
        private uint NewRandomUint() => (uint)new RandomNumberBetween(1, int.MaxValue);

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

        private static Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        protected TemplateFundingLine NewTemplateFundingLine(Action<TemplateFundingLineBuilder> setUp = null)
        {
            TemplateFundingLineBuilder templateFundingLineBuilder = new TemplateFundingLineBuilder();

            setUp?.Invoke(templateFundingLineBuilder);

            return templateFundingLineBuilder.Build();
        }
    }
}
