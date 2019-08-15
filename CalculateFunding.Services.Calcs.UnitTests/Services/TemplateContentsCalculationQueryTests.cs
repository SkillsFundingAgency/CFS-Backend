using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Calculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class TemplateContentsCalculationQueryTests : TemplateMappingTestBase
    {
        [TestMethod]
        [DynamicData(nameof(NestedFundingLineQueryExamples), DynamicDataSourceType.Method)]
        public void LocatesFirstMatchInFundingLineHierarchy(TemplateMappingItem mappingItem,
            TemplateMetadataContents templateContents,
            Calculation expectedCalculation)
        {
            Calculation actualCalculation = new TemplateContentsCalculationQuery()
                .GetTemplateContentsForMappingItem(mappingItem, templateContents);

            actualCalculation
                .Should()
                .BeSameAs(expectedCalculation);
        }

        public static IEnumerable<object[]> NestedFundingLineQueryExamples()
        {
            uint templateCalculationId = (uint) new RandomNumberBetween(1, int.MaxValue);

            Calculation expectedCalculation = NewTemplateMappingCalculation(_ => _.WithTemplateCalculationId(templateCalculationId));

            yield return new object[]
            {
                NewTemplateMappingItem(_ => _.WithTemplateId(templateCalculationId)),
                NewTemplateMetadataContents(_ => _.WithFundingLines(
                    NewFundingLine(fl => fl.WithCalculations(
                            expectedCalculation,
                            NewTemplateMappingCalculation())
                        .WithFundingLines(
                            NewFundingLine(fl1 => fl1.WithCalculations(
                                NewTemplateMappingCalculation(), 
                                NewTemplateMappingCalculation())))),
                    NewFundingLine(fl => fl.WithCalculations(NewTemplateMappingCalculation())))),
                expectedCalculation
            };
            yield return new object[]
            {
                NewTemplateMappingItem(_ => _.WithTemplateId(templateCalculationId)),
                NewTemplateMetadataContents(_ => _.WithFundingLines(
                    NewFundingLine(fl => fl.WithCalculations(
                            NewTemplateMappingCalculation(),
                            NewTemplateMappingCalculation())
                        .WithFundingLines(
                            NewFundingLine(fl1 => fl1.WithCalculations(
                                expectedCalculation,
                                NewTemplateMappingCalculation())))),
                    NewFundingLine(fl => fl.WithCalculations(NewTemplateMappingCalculation())))),
                expectedCalculation
            };
            yield return new object[]
            {
                NewTemplateMappingItem(_ => _.WithTemplateId(templateCalculationId)),
                NewTemplateMetadataContents(_ => _.WithFundingLines(
                    NewFundingLine(fl => fl.WithCalculations(
                            NewTemplateMappingCalculation(),
                            NewTemplateMappingCalculation())
                        .WithFundingLines(
                            NewFundingLine(fl1 => fl1.WithCalculations(
                                NewTemplateMappingCalculation(), 
                                NewTemplateMappingCalculation())))),
                    NewFundingLine(fl => fl.WithCalculations(expectedCalculation)))),
                expectedCalculation
            };
        }
    }
}