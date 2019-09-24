using System;
using System.Collections.Generic;
using CalculateFunding.Models.Publishing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishProviderExclusionCheckTests
    {
        [TestMethod]
        [DynamicData(nameof(ExcludeProviderExamples), DynamicDataSourceType.Method)]
        public void ExcludesProvidersWhereNoPaymentFundingLinesHaveNoneNullResults(GeneratedProviderResult generatedProviderResult,
            TemplateFundingLine[] flattenedTemplateFundingLines,
            bool expectedShouldBeExcludedResult)
        {
            PublishedProviderExclusionCheckResult checkResult = new PublishedProviderExclusionCheck()
                .ShouldBeExcluded(generatedProviderResult, flattenedTemplateFundingLines);

            checkResult
                .Should()
                .NotBeNull();

            checkResult
                .Should()
                .BeEquivalentTo(
                    new PublishedProviderExclusionCheckResult(generatedProviderResult.Provider.ProviderId,
                        expectedShouldBeExcludedResult));
        }

        public static IEnumerable<object[]> ExcludeProviderExamples()
        {
            const uint templateCalculationId = 28374;
            const uint templateLineId = 3453245;

            yield return new object[]
            {
                NewGeneratedProviderResult(gpr => gpr
                    .WithProvider(NewProvider())
                    .WithFundingCalculations(NewFundingCalculation(fc =>
                        fc.WithTemplateCalculationId(templateCalculationId)))
                    .WithFundlines(NewFundingLine(fl => fl.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                        .WithTemplateLineId(templateLineId)))),
                new[]
                {
                    NewTemplateFundingLine(fl => fl.WithTemplateLineId(templateLineId)
                        .WithCalculations(NewTemplateCalculation(tc => tc.WithTemplateCalculationId(templateCalculationId))))
                },
                true
            };
            yield return new object[]
            {
                NewGeneratedProviderResult(gpr => gpr
                    .WithProvider(NewProvider())
                    .WithFundingCalculations(NewFundingCalculation(fc =>
                        fc.WithTemplateCalculationId(templateCalculationId)
                            .WithValue(72364M)))
                    .WithFundlines(NewFundingLine(fl => fl.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                        .WithTemplateLineId(templateLineId)))),
                new[]
                {
                    NewTemplateFundingLine(fl => fl.WithTemplateLineId(templateLineId)
                        .WithCalculations(NewTemplateCalculation(tc => tc.WithTemplateCalculationId(templateCalculationId))))
                },
                false
            };
        }

        private static Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        private static TemplateCalculation NewTemplateCalculation(Action<TemplateCalcationBuilder> setUp = null)
        {
            TemplateCalcationBuilder templateCalculationBuilder = new TemplateCalcationBuilder();

            setUp?.Invoke(templateCalculationBuilder);

            return templateCalculationBuilder.Build();
        }

        private static TemplateFundingLine NewTemplateFundingLine(Action<TemplateFundingLineBuilder> setUp = null)
        {
            TemplateFundingLineBuilder builder = new TemplateFundingLineBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private static GeneratedProviderResult NewGeneratedProviderResult(Action<GeneratedProviderResultBuilder> setUp = null)
        {
            GeneratedProviderResultBuilder generatedProviderResultBuilder = new GeneratedProviderResultBuilder();

            setUp?.Invoke(generatedProviderResultBuilder);

            return generatedProviderResultBuilder.Build();
        }

        private static FundingCalculation NewFundingCalculation(Action<FundingCalculationBuilder> setUp = null)
        {
            FundingCalculationBuilder fundingCalculationBuilder = new FundingCalculationBuilder();

            setUp?.Invoke(fundingCalculationBuilder);

            return fundingCalculationBuilder.Build();
        }

        private static FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }
    }
}