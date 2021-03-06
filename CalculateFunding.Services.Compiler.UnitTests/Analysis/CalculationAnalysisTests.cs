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

namespace CalculateFunding.Services.Compiler.UnitTests.Analysis
{
    [TestClass]
    public class CalculationAnalysisTests
    {
        private CalculationAnalysis _analysis;
        private IEnumerable<CalculationRelationship> _relationships;
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
            Action invocation = () => WhenTheCalculationsAreAnalysed(calculations);

            invocation
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        [DynamicData(nameof(CalculationRelationshipExamples), DynamicDataSourceType.Method)]
        public void LocatesRelatedCalculationsFromSourceCode(IEnumerable<Calculation> calculations,
            IEnumerable<CalculationRelationship> expectedCalculationRelationships)
        {
            WhenTheCalculationsAreAnalysed(calculations);

            ThenTheCalculationRelationshipsShouldMatch(expectedCalculationRelationships);
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

        private void ThenTheFundingLineCalculationRelationShipsShouldMatch(IEnumerable<FundingLineCalculationRelationship> expectedFundingLineRelationships)
        {
            _fundingLineRelationships
                .Should()
                .NotBeNull();

            _fundingLineRelationships
                .Should()
                .BeEquivalentTo(expectedFundingLineRelationships);
        }

        private void WhenTheCalculationsAreAnalysed(IEnumerable<Calculation> calculations)
        {
            _relationships = _analysis.DetermineRelationshipsBetweenCalculations(calculations);
        }

        private void WhenTheFundingLinesAreAnalysed(IEnumerable<Calculation> calculations, IDictionary<string, Funding> funding)
        {
            _fundingLineRelationships = _analysis.DetermineRelationshipsBetweenFundingLinesAndCalculations(calculations, funding);
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
            string calcOneName = nameof(calcOneName);
            string calcOneId = NewRandomString();
            string calcTwoName = nameof(calcTwoName);
            string calcTwoId = NewRandomString();
            string calcThreeName = nameof(calcThreeName);
            string calcThreeId = NewRandomString();
            string calcFourName = nameof(calcFourName);
            string calcFourId = NewRandomString();
            string @namespace = "fundingStreamId";
            Models.Graph.FundingLine fundingLine2 = new Models.Graph.FundingLine { FundingLineId = $"{@namespace}_{fundingLineTwoId}", FundingLineName = fundingLineTwoName };

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
                        .WithId(calcOneId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcTwoName)
                                .WithSourceCode($"return Calculations.{calcOneName}() + 20")))
                        .WithId(calcTwoId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcThreeName)
                                .WithSourceCode($"return {@namespace}.FundingLines.{fundingLineTwoName}()")))
                        .WithId(calcThreeId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcFourName)
                                .WithSourceCode("return 0")))
                        .WithId(calcFourId))
                ),
                fundings,
                new FundingLineCalculationRelationship[] { new FundingLineCalculationRelationship {CalculationOneId = calcThreeId, CalculationTwoId = calcOneId, FundingLine = fundingLine2 } }
            };
            yield return new object[]
            {
                Calculations(NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcOneName)
                                .WithSourceCode("return 10")))
                        .WithId(calcOneId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcTwoName)
                                .WithSourceCode($"return Calculations.{calcOneName}() + 20")))
                        .WithId(calcTwoId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcThreeName)
                                .WithSourceCode("return 10")))
                        .WithId(calcThreeId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcFourName)
                                .WithSourceCode($"return {@namespace}.FundingLines.{fundingLineTwoName}()")))
                        .WithId(calcFourId))
                ),
                fundings,
                new FundingLineCalculationRelationship[] { new FundingLineCalculationRelationship {CalculationOneId = calcFourId, CalculationTwoId = calcOneId, FundingLine = fundingLine2 } }
            };
            yield return new object[]
            {
                Calculations(NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcOneName)
                                .WithSourceCode("return 10")))
                        .WithId(calcOneId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcTwoName)
                                .WithSourceCode($"return 20")))
                        .WithId(calcTwoId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcThreeName)
                                .WithSourceCode($"return 10")))
                        .WithId(calcThreeId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcFourName)
                                .WithSourceCode("return 0")))
                        .WithId(calcFourId))
                ),
                fundings,
                new FundingLineCalculationRelationship[0]
            };
        }

        private static IEnumerable<object[]> CalculationRelationshipExamples()
        {
            string calcOneName = nameof(calcOneName);
            string calcOneId = NewRandomString();
            string calcTwoName = nameof(calcTwoName);
            string calcTwoId = NewRandomString();
            string calcThreeName = nameof(calcThreeName);
            string calcThreeId = NewRandomString();
            string calcFourName = nameof(calcFourName);
            string calcFourId = NewRandomString();

            yield return new object[]
            {
                Calculations(NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcOneName)
                                .WithSourceCode("return 10")))
                        .WithId(calcOneId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcTwoName)
                                .WithSourceCode($"return Calculations.{calcOneName}() + 20")))
                        .WithId(calcTwoId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcThreeName)
                                .WithSourceCode($"return Calculations.{calcOneName}() * Calculations.{calcFourName}() + 20")))
                        .WithId(calcThreeId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcFourName)
                                .WithSourceCode("return 0")))
                        .WithId(calcFourId))
                ),
                CalculationRelationships(NewCalculationRelationship(_ => _.WithCalculationOneId(calcTwoId)
                        .WithCalculationTwoId(calcOneId)),
                    NewCalculationRelationship(_ => _.WithCalculationOneId(calcThreeId)
                        .WithCalculationTwoId(calcOneId)),
                    NewCalculationRelationship(_ => _.WithCalculationOneId(calcThreeId)
                        .WithCalculationTwoId(calcFourId)))
            };
            yield return new object[]
            {
                Calculations(NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcOneName)
                                .WithSourceCode("return 10")))
                        .WithId(calcOneId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcTwoName)
                                .WithSourceCode($"return Calculations.{calcOneName}() + 20")))
                        .WithId(calcTwoId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcThreeName)
                                .WithSourceCode($"return 10")))
                        .WithId(calcThreeId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcFourName)
                                .WithSourceCode("return 0")))
                        .WithId(calcFourId))
                ),
                CalculationRelationships(NewCalculationRelationship(_ => _.WithCalculationOneId(calcTwoId)
                        .WithCalculationTwoId(calcOneId)))
            };
            yield return new object[]
            {
                Calculations(NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcOneName)
                                .WithSourceCode("return 10")))
                        .WithId(calcOneId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcTwoName)
                                .WithSourceCode($"return 20")))
                        .WithId(calcTwoId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcThreeName)
                                .WithSourceCode($"return 10")))
                        .WithId(calcThreeId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcFourName)
                                .WithSourceCode("return 0")))
                        .WithId(calcFourId))
                ),
                new CalculationRelationship[0]
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

        private static CalculationRelationship[] CalculationRelationships(params CalculationRelationship[] calculationRelationships)
        {
            return calculationRelationships;
        }

        private static IEnumerable<object[]> MissingCalculationExamples()
        {
            yield return new[] {(object) null};
            yield return new object[] {new Calculation[0]};
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

        private static string NewRandomString()
        {
            return new RandomString();
        }
    }
}