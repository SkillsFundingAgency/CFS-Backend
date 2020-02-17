using System;
using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Compiler.Analysis;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Calculation = CalculateFunding.Models.Calcs.Calculation;

namespace CalculateFunding.Services.Compiler.UnitTests.Analysis
{
    [TestClass]
    public class CalculationAnalysisTests
    {
        private CalculationAnalysis _analysis;
        private IEnumerable<CalculationRelationship> _relationships;

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

        private void ThenTheCalculationRelationshipsShouldMatch(IEnumerable<CalculationRelationship> expectedCalculationRelationships)
        {
            _relationships
                .Should()
                .NotBeNull();

            _relationships
                .Should()
                .BeEquivalentTo(expectedCalculationRelationships);
        }

        private void WhenTheCalculationsAreAnalysed(IEnumerable<Calculation> calculations)
        {
            _relationships = _analysis.DetermineRelationshipsBetweenCalculations(calculations);
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
                                .WithSourceCode($"return {calcOneName}() + 20")))
                        .WithId(calcTwoId)),
                    NewCalculation(_ => _.WithCurrentVersion(NewCalculationVersion(cv
                            => cv.WithSourceCodeName(calcThreeName)
                                .WithSourceCode($"return {calcOneName}() * {calcFourName}() + 20")))
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
                                .WithSourceCode($"return {calcOneName}() + 20")))
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