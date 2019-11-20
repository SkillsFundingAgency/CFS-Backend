using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.TemplateMetadata.Schema10;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
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

        [TestMethod]
        [DataRow(132, "Universal Entitlement for 3 and 4 Year Olds total Early Years Universal Entitlement for 3 and 4 Year Olds Rate", Common.TemplateMetadata.Enums.CalculationValueFormat.Currency, Common.TemplateMetadata.Enums.CalculationType.Rate, Common.TemplateMetadata.Enums.AggregationType.Average, "")]
        public void LocatesFirstMatchInFundingLineHeirachyForDSG(int templateCalculationId, string calcName, Common.TemplateMetadata.Enums.CalculationValueFormat calculationValueFormat, Common.TemplateMetadata.Enums.CalculationType calculationType, Common.TemplateMetadata.Enums.AggregationType aggregationType, string formulaText)
        {
            ILogger logger = CreateLogger();

            ITemplateMetadataGenerator templateMetaDataGenerator = CreateTemplateGenerator(logger);

            TemplateMetadataContents templateContents = templateMetaDataGenerator.GetMetadata(GetResourceString("CalculateFunding.Services.Calcs.UnitTests.Resources.DSG1.0.json"));

            Calculation actualCalculation = new TemplateContentsCalculationQuery()
                .GetTemplateContentsForMappingItem(NewTemplateMappingItem(_ => _.WithTemplateId((uint)templateCalculationId))
                , templateContents);

            Calculation expectedCalculation = NewTemplateMappingCalculation(_ => _.WithTemplateCalculationId((uint)templateCalculationId)
            .WithName(calcName)
            .WithValueFormat(calculationValueFormat)
            .WithType(calculationType)
            .WithAggregationType(aggregationType)
            .WithFormulaText(formulaText));

            actualCalculation
                .Should()
                .BeEquivalentTo(expectedCalculation);
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

        public ITemplateMetadataGenerator CreateTemplateGenerator(ILogger logger = null)
        {
            return new TemplateMetadataGenerator(logger ?? CreateLogger());
        }
        public ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        public string GetResourceString(string resourceName)
        {
            return typeof(TemplateContentsCalculationQueryTests)
                .Assembly
                .GetEmbeddedResourceFileContents(resourceName);
        }
    }
}