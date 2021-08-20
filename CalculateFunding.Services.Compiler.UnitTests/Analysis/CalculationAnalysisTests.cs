using System;
using System.Collections.Generic;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Compiler.Analysis;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalculationVersion = CalculateFunding.Models.Calcs.CalculationVersion;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using Funding = CalculateFunding.Models.Calcs.Funding;
using FundingLine = CalculateFunding.Models.Calcs.FundingLine;
using FundingLineCalculation = CalculateFunding.Models.Calcs.FundingLineCalculation;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;

namespace CalculateFunding.Services.Compiler.UnitTests.Analysis
{
    [TestClass]
    public class CalculationAnalysisTests
    {
        private CalculationAnalysis _analysis;
        private IEnumerable<CalculationRelationship> _relationships;
        private IEnumerable<CalculationRelationship> _releasedDataCalculationRelationships;
        private IEnumerable<CalculationEnumRelationship> _enumRelationships;
        private IEnumerable<FundingLineCalculationRelationship> _fundingLineRelationships;

        [TestInitialize]
        public void SetUp()
        {
            _analysis = new CalculationAnalysis();
        }

        [TestMethod]
        [DynamicData(nameof(MissingCalculationExamples), DynamicDataSourceType.Method)]
        public void ThrowsArgumentExceptionIfNoCalculationsSuppliedToAnalyse(IEnumerable<Calculation> calculations)
        {
            Action invocation = () => WhenTheCalculationsAreAnalysed(calculations, null, null);

            invocation
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        [DynamicData(nameof(CalculationRelationshipExamples), DynamicDataSourceType.Method)]
        public void LocatesRelatedCalculationsFromSourceCode(
            IEnumerable<Calculation> calculations,
            IEnumerable<DatasetRelationshipSummary> datasetRelationshipSummaries,
            IEnumerable<TemplateMapping> templateMappings,
            IEnumerable<CalculationRelationship> expectedCalculationRelationships,
            IEnumerable<CalculationRelationship> expectedReleasedDataCalculationRelationships,
            IEnumerable<CalculationEnumRelationship> expectedEnumCalculationRelationships)
        {
            WhenTheCalculationsAreAnalysed(calculations, datasetRelationshipSummaries, templateMappings);

            ThenTheCalculationRelationshipsShouldMatch(expectedCalculationRelationships);

            ThenTheReleasedDataCalculationRelationshipsShouldMatch(expectedReleasedDataCalculationRelationships);

            ThenTheCalculationEnumRelationshipsShouldMatch(expectedEnumCalculationRelationships);
        }

        [TestMethod]
        [DynamicData(nameof(FundingLineRelationshipExamples), DynamicDataSourceType.Method)]
        public void LocatesRelatedFundinglinesAndCalculationsFromSourceCode(IEnumerable<Calculation> calculations,
            Dictionary<string, Funding> expectedFunding,
            IEnumerable<FundingLineCalculationRelationship> expectedFundingLineRelationships)
        {
            WhenTheFundingLinesAreAnalysed(calculations, expectedFunding);

            ThenTheFundingLineCalculationRelationShipsShouldMatch(expectedFundingLineRelationships);
        }

        private void ThenTheCalculationRelationshipsShouldMatch(IEnumerable<CalculationRelationship> expectedCalculationRelationships)
        {
            _relationships
                .Should()
                .NotBeNull();

            _relationships
                .Should()
                .BeEquivalentTo(expectedCalculationRelationships);
        }

        private void ThenTheReleasedDataCalculationRelationshipsShouldMatch(IEnumerable<CalculationRelationship> expectedReleasedDataCalculationRelationships)
        {
            _releasedDataCalculationRelationships
                .Should()
                .NotBeNull();

            _releasedDataCalculationRelationships
                .Should()
                .BeEquivalentTo(expectedReleasedDataCalculationRelationships);
        }

        private void ThenTheCalculationEnumRelationshipsShouldMatch(IEnumerable<CalculationEnumRelationship> expectedEnumCalculationRelationships)
        {
            _enumRelationships
                .Should()
                .NotBeNull();

            _enumRelationships
                .Should()
                .BeEquivalentTo(expectedEnumCalculationRelationships);
        }

        private void ThenTheFundingLineCalculationRelationShipsShouldMatch(IEnumerable<FundingLineCalculationRelationship> expectedFundingLineRelationships)
        {
            _fundingLineRelationships
                .Should()
                .NotBeNull();

            _fundingLineRelationships
                .Should()
                .BeEquivalentTo(expectedFundingLineRelationships);
        }

        private void WhenTheCalculationsAreAnalysed(
            IEnumerable<Calculation> calculations,
            IEnumerable<DatasetRelationshipSummary> datasetRelationshipSummaries,
            IEnumerable<TemplateMapping> templateMappings)
        {
            _relationships = _analysis.DetermineRelationshipsBetweenCalculations(@namespace => @namespace, calculations);

            _releasedDataCalculationRelationships = 
                _analysis.DetermineRelationshipsBetweenReleasedDataCalculations(@namespace => @namespace, calculations, datasetRelationshipSummaries, templateMappings);

            _enumRelationships = _analysis.DetermineRelationshipsBetweenCalculationsAndEnums(calculations);
        }

        private void WhenTheFundingLinesAreAnalysed(IEnumerable<Calculation> calculations, IDictionary<string, Funding> funding)
        {
            _fundingLineRelationships = _analysis.DetermineRelationshipsBetweenFundingLinesAndCalculations(@namespace => @namespace, calculations, funding);
        }

        private static IEnumerable<object[]> FundingLineRelationshipExamples()
        {
            string fundingLineOneName = nameof(fundingLineOneName);
            uint fundingLineOneId = 1;
            string fundingLineTwoName = nameof(fundingLineTwoName);
            uint fundingLineTwoId = 2;
            string fundingLineThreeName = nameof(fundingLineThreeName);
            uint fundingLineThreeId = 3;
            string fundingLineFourName = nameof(fundingLineFourName);
            uint fundingLineFourId = 4;
            string specificationId = NewRandomString();
            string calcOneName = nameof(calcOneName);
            string calcOneId = NewRandomString();
            string calcTwoName = nameof(calcTwoName);
            string calcTwoId = NewRandomString();
            string calcThreeName = nameof(calcThreeName);
            string calcThreeId = NewRandomString();
            string calcFourName = nameof(calcFourName);
            string calcFourId = NewRandomString();
            string @namespace = "fundingStreamId";
            Models.Graph.FundingLine fundingLine2 = new Models.Graph.FundingLine { SpecificationId = specificationId, FundingLineId = $"{@namespace}_{fundingLineTwoId}", FundingLineName = fundingLineTwoName };

            Dictionary<string, Funding> fundings = new Dictionary<string, Funding> {
                            {@namespace, new Funding{
                                FundingLines = FundingLines(NewFundingLine(_ => _.WithTemplateId(fundingLineOneId)
                                .WithName(fundingLineOneName)
                                .WithNamespace(@namespace)
                                .WithSourceCodeName(fundingLineOneName)),
                                NewFundingLine(_ => _.WithTemplateId(fundingLineTwoId)
                                .WithName(fundingLineTwoName)
                                .WithCalculations(new FundingLineCalculation[] {new FundingLineCalculation{Id = 1} })
                                .WithNamespace(@namespace)
                                .WithSourceCodeName(fundingLineTwoName)),
                                NewFundingLine(_ => _.WithTemplateId(fundingLineThreeId)
                                .WithName(fundingLineThreeName)
                                .WithNamespace(@namespace)
                                .WithSourceCodeName(fundingLineThreeName)),
                                NewFundingLine(_ => _.WithTemplateId(fundingLineFourId)
                                .WithName(fundingLineFourName)
                                .WithNamespace(@namespace)
                                .WithSourceCodeName(fundingLineFourName))),
                                Mappings = new Dictionary<uint, string> { { 1, calcOneId } }
                            }
                        }
                    };

            yield return new object[]
            {
                Calculations(NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcOneName)
                                .WithSourceCode("return 10")))
                        .WithId(calcOneId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcTwoName)
                                .WithSourceCode($"return Calculations.{calcOneName}() + 20")))
                        .WithId(calcTwoId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcThreeName)
                                .WithSourceCode($"return {@namespace}.FundingLines.{fundingLineTwoName}()")))
                        .WithId(calcThreeId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcFourName)
                                .WithSourceCode("return 0")))
                        .WithId(calcFourId)
                        .WithSpecificationId(specificationId))
                ),
                fundings,
                new FundingLineCalculationRelationship[] { new FundingLineCalculationRelationship {CalculationOneId = calcThreeId, CalculationTwoId = calcOneId, FundingLine = fundingLine2 } }
            };
            yield return new object[]
            {
                Calculations(NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcOneName)
                                .WithSourceCode("return 10")))
                        .WithId(calcOneId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcTwoName)
                                .WithSourceCode($"return Calculations.{calcOneName}() + 20")))
                        .WithId(calcTwoId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcThreeName)
                                .WithSourceCode("return 10")))
                        .WithId(calcThreeId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcFourName)
                                .WithSourceCode($"return {@namespace}.FundingLines.{fundingLineTwoName}()")))
                        .WithId(calcFourId)
                        .WithSpecificationId(specificationId))
                ),
                fundings,
                new FundingLineCalculationRelationship[] { new FundingLineCalculationRelationship {CalculationOneId = calcFourId, CalculationTwoId = calcOneId, FundingLine = fundingLine2 } }
            };
            yield return new object[]
            {
                Calculations(NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcOneName)
                                .WithSourceCode("return 10")))
                        .WithId(calcOneId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcTwoName)
                                .WithSourceCode($"return 20")))
                        .WithId(calcTwoId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcThreeName)
                                .WithSourceCode($"return 10")))
                        .WithId(calcThreeId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcFourName)
                                .WithSourceCode("return 0")))
                        .WithId(calcFourId)
                        .WithSpecificationId(specificationId))
                ),
                fundings,
                Array.Empty<FundingLineCalculationRelationship>()
            };
        }

        private static IEnumerable<object[]> CalculationRelationshipExamples()
        {
            string specificationId = NewRandomString();
            string calcOneName = nameof(calcOneName);
            string calcOneId = NewRandomString();
            string calcTwoName = nameof(calcTwoName);
            string calcTwoId = NewRandomString();
            string calcThreeName = nameof(calcThreeName);
            string calcThreeId = NewRandomString();
            string calcFourName = nameof(calcFourName);
            string calcFourId = NewRandomString();
            string calcFiveName = nameof(calcFiveName);
            string calcFiveId = NewRandomString();
            string enumValue = NewRandomString();

            string targetSpecificationName = "Spec12345";
            string targetSpecificationId = NewRandomString();

            uint templateCalculationOneId = 1;

            Calculation calculation = NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcOneName)
                                .WithDataType(CalculationDataType.Enum)
                                .WithSourceCode($"return {calcOneName}Options.{enumValue}")))
                        .WithId(calcOneId)
                        .WithSpecificationId(specificationId));

            yield return new object[]
            {
                Calculations(calculation,
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcTwoName)
                                .WithSourceCode($"return Calculations.{calcOneName}() + 20")))
                        .WithId(calcTwoId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcThreeName)
                                .WithSourceCode($"return Calculations.{calcOneName}() * Calculations.{calcFourName}() + 20")))
                        .WithId(calcThreeId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcFourName)
                                .WithSourceCode("return 0")))
                        .WithId(calcFourId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcFiveName)
                                .WithSourceCode($"return Datasets.{targetSpecificationName}.Calc_{templateCalculationOneId}_{calcFourName}")))
                        .WithId(calcFiveId)
                        .WithSpecificationId(specificationId))
                ),
                DatasetRelationshipSummaries(
                    NewDatasetRelationshipSummary(_ => _
                        .WithTargetSpecificationName(targetSpecificationName)
                        .WithTargetSpecificationId(targetSpecificationId)
                        .WithPublishedSpecificationConfigurationCalculations(
                            PublishedSpecificationItems(
                                NewPublishedSpecificationItem(psi => psi
                                    .WithTemplateId(templateCalculationOneId)
                                    .WithSourceCodeName(calcFourName)))))),
                TemplateMappings(
                    NewTemplateMapping(_ => _
                        .WithSpecificationId(targetSpecificationId)
                        .WithItems(
                            NewTemplateMappingItem(tmi => tmi
                                .WithTemplateId(templateCalculationOneId)
                                .WithCalculationId(calcFourId))))),
                CalculationRelationships(NewCalculationRelationship(_ => _.WithCalculationOneId(calcTwoId)
                        .WithCalculationTwoId(calcOneId)),
                    NewCalculationRelationship(_ => _.WithCalculationOneId(calcThreeId)
                        .WithCalculationTwoId(calcOneId)),
                    NewCalculationRelationship(_ => _.WithCalculationOneId(calcThreeId)
                        .WithCalculationTwoId(calcFourId))),
                CalculationRelationships(
                    NewCalculationRelationship(_ => _
                        .WithCalculationOneId(calcFiveId)
                        .WithCalculationTwoId(calcFourId))),
                CalulationEnumRelationships(NewCalculationEnumRelationship(_ => _.WithCalculation(new Models.Graph.Calculation{SpecificationId = calculation.SpecificationId,
                            CalculationId = calculation.Id,
                            FundingStream = calculation.FundingStreamId,
                            CalculationName = calculation.Name,
                            CalculationType = calculation.Current.CalculationType == Models.Calcs.CalculationType.Additional ?
                                                                                        Models.Graph.CalculationType.Additional :
                                                                                        Models.Graph.CalculationType.Template })
                    .WithEnum(new Models.Graph.Enum{ 
                        EnumName = $"{calcOneName}Options",
                        EnumValue = enumValue,
                        SpecificationId = specificationId
                    })))
            };
            yield return new object[]
            {
                Calculations(NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcOneName)
                                .WithSourceCode("return 10")))
                        .WithId(calcOneId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcTwoName)
                                .WithSourceCode($"return Calculations.{calcOneName}() + 20")))
                        .WithId(calcTwoId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcThreeName)
                                .WithSourceCode($"return 10")))
                        .WithId(calcThreeId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcFourName)
                                .WithSourceCode("return 0")))
                        .WithId(calcFourId)
                        .WithSpecificationId(specificationId))
                ),
                Array.Empty<DatasetRelationshipSummary>(),
                Array.Empty<TemplateMapping>(),
                CalculationRelationships(NewCalculationRelationship(_ => _.WithCalculationOneId(calcTwoId)
                        .WithCalculationTwoId(calcOneId))),
                Array.Empty<CalculationRelationship>(),
                Array.Empty<CalculationEnumRelationship>()
            };
            yield return new object[]
            {
                Calculations(NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcOneName)
                                .WithSourceCode("return 10")))
                        .WithId(calcOneId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcTwoName)
                                .WithSourceCode($"return 20")))
                        .WithId(calcTwoId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcThreeName)
                                .WithSourceCode($"return 10")))
                        .WithId(calcThreeId)
                        .WithSpecificationId(specificationId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcFourName)
                                .WithSourceCode("return 0")))
                        .WithId(calcFourId)
                        .WithSpecificationId(specificationId))
                ),
                Array.Empty<DatasetRelationshipSummary>(),
                Array.Empty<TemplateMapping>(),
                Array.Empty<CalculationRelationship>(),
                Array.Empty<CalculationRelationship>(),
                Array.Empty<CalculationEnumRelationship>()
            };
        }

        private static Calculation[] Calculations(params Calculation[] calculations)
        {
            return calculations;
        }

        private static FundingLine[] FundingLines(params FundingLine[] fundingLines)
        {
            return fundingLines;
        }

        private static DatasetRelationshipSummary[] DatasetRelationshipSummaries(params DatasetRelationshipSummary[] datasetRelationshipSummaries)
        {
            return datasetRelationshipSummaries;
        }

        private static TemplateMapping[] TemplateMappings(params TemplateMapping[] templateMappings)
        {
            return templateMappings;
        }

        private static CalculationRelationship[] CalculationRelationships(params CalculationRelationship[] calculationRelationships)
        {
            return calculationRelationships;
        }

        private static CalculationEnumRelationship[] CalulationEnumRelationships(params CalculationEnumRelationship[] calculationEnumRelationships)
        {
            return calculationEnumRelationships;
        }

        private static PublishedSpecificationItem[] PublishedSpecificationItems(params PublishedSpecificationItem[] publishedSpecificationItems)
        {
            return publishedSpecificationItems;
        }

        private static IEnumerable<object[]> MissingCalculationExamples()
        {
            yield return new[] {(object) null};
            yield return new object[] { Array.Empty<Calculation>() };
        }

        private static Calculation NewCalculation(Action<CalculationBuilder> setUp = null)
        {
            CalculationBuilder calculationBuilder = new CalculationBuilder();

            setUp?.Invoke(calculationBuilder);

            return calculationBuilder.Build();
        }
        private static FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        private static CalculationVersion NewCalculationVersion(Action<CalculationVersionBuilder> setUp = null)
        {
            CalculationVersionBuilder calculationVersionBuilder = new CalculationVersionBuilder();

            setUp?.Invoke(calculationVersionBuilder);

            return calculationVersionBuilder.Build();
        }

        private static CalculationRelationship NewCalculationRelationship(Action<CalculationRelationshipBuilder> setUp = null)
        {
            CalculationRelationshipBuilder calculationRelationshipBuilder = new CalculationRelationshipBuilder();

            setUp?.Invoke(calculationRelationshipBuilder);

            return calculationRelationshipBuilder.Build();
        }

        private static DatasetRelationshipSummary NewDatasetRelationshipSummary(Action<DatasetRelationshipSummaryBuilder> setUp = null)
        {
            DatasetRelationshipSummaryBuilder datasetRelationshipSummaryBuilder = new DatasetRelationshipSummaryBuilder();

            setUp?.Invoke(datasetRelationshipSummaryBuilder);

            return datasetRelationshipSummaryBuilder.Build();
        }

        private static PublishedSpecificationConfiguration NewPublishedSpecificationConfiguration(Action<PublishedSpecificationConfigurationBuilder> setUp = null)
        {
            PublishedSpecificationConfigurationBuilder publishedSpecificationConfigurationBuilder = new PublishedSpecificationConfigurationBuilder();

            setUp?.Invoke(publishedSpecificationConfigurationBuilder);

            return publishedSpecificationConfigurationBuilder.Build();
        }

        private static PublishedSpecificationItem NewPublishedSpecificationItem(Action<PublishedSpecificationItemBuilder> setUp = null)
        {
            PublishedSpecificationItemBuilder publishedSpecificationItemBuilder = new PublishedSpecificationItemBuilder();

            setUp?.Invoke(publishedSpecificationItemBuilder);

            return publishedSpecificationItemBuilder.Build();
        }

        private static TemplateMapping NewTemplateMapping(Action<TemplateMappingBuilder> setUp = null)
        {
            TemplateMappingBuilder templateMappingBuilder = new TemplateMappingBuilder();

            setUp?.Invoke(templateMappingBuilder);

            return templateMappingBuilder.Build();
        }

        private static TemplateMappingItem NewTemplateMappingItem(Action<TemplateMappingItemBuilder> setUp = null)
        {
            TemplateMappingItemBuilder templateMappingItemBuilder = new TemplateMappingItemBuilder();

            setUp?.Invoke(templateMappingItemBuilder);

            return templateMappingItemBuilder.Build();
        }

        private static CalculationEnumRelationship NewCalculationEnumRelationship(Action<CalculationEnumRelationshipBuilder> setUp = null)
        {
            CalculationEnumRelationshipBuilder calculationEnumRelationshipBuilder = new CalculationEnumRelationshipBuilder();

            setUp?.Invoke(calculationEnumRelationshipBuilder);

            return calculationEnumRelationshipBuilder.Build();
        }

        private static string NewRandomString()
        {
            return new RandomString();
        }
    }
}