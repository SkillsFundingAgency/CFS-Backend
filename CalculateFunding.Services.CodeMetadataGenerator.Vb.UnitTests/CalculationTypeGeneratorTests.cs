using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.CodeMetadataGenerator.Vb.UnitTests
{
    [TestClass]
    public class CalculationTypeGeneratorTests
    {
        [TestMethod]
        [DataRow("Range 3", "Range3")]
        [DataRow("Range < 3", "RangeLessThan3")]
        [DataRow("Range > 3", "RangeGreaterThan3")]
        [DataRow("Range £ 3", "RangePound3")]
        [DataRow("Range % 3", "RangePercent3")]
        [DataRow("Range = 3", "RangeEquals3")]
        [DataRow("Range + 3", "RangePlus3")]
        [DataRow("Nullable(Of Decimal)", "Nullable(Of Decimal)")]
        [DataRow("Nullable(Of Integer)", "Nullable(Of Integer)")]
        public void GenerateIdentifier_IdentifiersSubstituted(string input, string expected)
        {
            // Act
            string result = CalculationTypeGenerator.GenerateIdentifier(input);

            // Assert
            result
                .Should()
                .Be(expected);
        }

        [TestMethod]
        [DataRow("return calc1()", "return calc1()")]
        [DataRow("return AvgTest()", "return AvgTest()")]
        [DataRow("return SumTest()", "return SumTest()")]
        [DataRow("return TestMinTest()", "return TestMinTest()")]
        [DataRow("return Avg(calc1)", "return Avg(\"calc1\")")]
        [DataRow("return Max(calc1)", "return Max(\"calc1\")")]
        [DataRow("return Min(calc1)", "return Min(\"calc1\")")]
        [DataRow("return Sum(calc1)", "return Sum(\"calc1\")")]
        [DataRow("return    Max(calc1)", "return    Max(\"calc1\")")]
        [DataRow("return TestSum() + Max(calc1)", "return TestSum() + Max(\"calc1\")")]
        [DataRow("return    Max(calc1) + Sum(calc1)", "return    Max(\"calc1\") + Sum(\"calc1\")")]
        [DataRow("return    Max( calc1     ) + Sum(    calc1    )", "return    Max(\"calc1\") + Sum(\"calc1\")")]
        [DataRow("If(InpAdjPupilNumberGuaranteed= \"YES\" and InputAdjNOR > NOREstRtoY11) then", "If(InpAdjPupilNumberGuaranteed= \"YES\" and InputAdjNOR > NOREstRtoY11) then")]
        [DataRow("FundingRate1 = -1 * (Math.Min(ThresholdZ, FundingRateZ) / FundingRateZ) * Condition1 + Sum(test1)", "FundingRate1 = -1 * (Math.Min(ThresholdZ, FundingRateZ) / FundingRateZ) * Condition1 + Sum(\"test1\")")]
        public void QuoteAggregateFunctionCalls_QuotesAsExpected(string input, string expected)
        {
            //Act
            string result = CalculationTypeGenerator.QuoteAggregateFunctionCalls(input);

            //Assert
            result
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void GenerateCalcs_GivenCalculationsAndCompilerOptionsStrictOn_ThenOptionStrictGenerated()
        {
            // Arrange
            List<Calculation> calculations = new List<Calculation>();
            Dictionary<string, Funding> fundingLines = new Dictionary<string, Funding>();

            CompilerOptions compilerOptions = new CompilerOptions
            {

                OptionStrictEnabled = true
            };

            CalculationTypeGenerator calculationTypeGenerator = new CalculationTypeGenerator(compilerOptions);

            // Act
            IEnumerable<SourceFile> results = calculationTypeGenerator.GenerateCalcs(calculations, fundingLines);

            // Assert
            results.Should().HaveCount(1);
            results.First().SourceCode.Should().StartWith("Option Strict On");
        }

        [TestMethod]
        public void GenerateCalcs_GivenCalculationsAndCompilerOptionsOff_ThenOptionsGenerated()
        {
            // Arrange
            List<Calculation> calculations = new List<Calculation>();
            Dictionary<string, Funding> fundingLines = new Dictionary<string, Funding>();

            CompilerOptions compilerOptions = new CompilerOptions
            {
                OptionStrictEnabled = false
            };

            CalculationTypeGenerator calculationTypeGenerator = new CalculationTypeGenerator(compilerOptions);

            // Act
            IEnumerable<SourceFile> results = calculationTypeGenerator.GenerateCalcs(calculations, fundingLines);

            // Assert
            results.Should().HaveCount(1);
            results.First().SourceCode.Should().StartWith("Option Strict Off");
        }

        [TestMethod]
        public void GenerateCalculations_GivenEnumCalculation_ThenEnumCreated()
        {
            Calculation _1619 = NewCalculation(_ => _.WithFundingStream(NewReference(rf => rf.WithId("1619")))
                .WithSourceCodeName("One")
                .WithCalculationNamespaceType(CalculationNamespace.Template)
                .WithSourceCode("return 456")
                .WithName("EnumName")
                .WithDataType(CalculationDataType.Enum)
                .WithAllowedEnumTypeValues(new[] { "Option1", "Option2" }));
            
            FundingLine _1619fl = NewFundingLine(_ => _.WithCalculations(new[] { NewFundingLineCalculation(_ => _.WithId(1)
                .WithCalculationNamespaceType(CalculationNamespace.Template))})
                .WithId(1)
                .WithName("One")
                .WithSourceCodeName("One")
                .WithNamespace("1619"));

            IDictionary<string, Funding> fundingLines = new Dictionary<string, Funding> {
                {"1619", NewFunding(_ => _.WithFundingLines(new[] { _1619fl }))},
            };

            IEnumerable<SourceFile> results = new CalculationTypeGenerator(new CompilerOptions()).GenerateCalcs(new[] { _1619}, fundingLines);

            results.Should().HaveCount(1);
            results.First()
                .SourceCode
                .Should()
                .Contain(
@$"Enum EnumNameOptions
        Option1
        Option2
    End Enum");
        }

        [TestMethod]
        public void GenerateCalculations_GivenNoAdditionalCalculations_ThenSingleInnerClassForAdditionalStillCreated()
        {
            Calculation _1619 = NewCalculation(_ => _.WithFundingStream(NewReference(rf => rf.WithId("1619")))
                .WithSourceCodeName("One")
                .WithCalculationNamespaceType(CalculationNamespace.Template)
                .WithSourceCode("return 456"));
            Calculation psg = NewCalculation(_ => _.WithFundingStream(NewReference(rf => rf.WithId("PSG")))
                .WithCalculationNamespaceType(CalculationNamespace.Template)
                .WithSourceCodeName("One")
                .WithSourceCode("return DSG.One() + 100"));

            FundingLine _1619fl = NewFundingLine(_ => _.WithCalculations(new[] { NewFundingLineCalculation(_ => _.WithId(1)
                .WithCalculationNamespaceType(CalculationNamespace.Template))})
                .WithId(1)
                .WithName("One")
                .WithSourceCodeName("One")
                .WithNamespace("1619"));

            FundingLine psgfl = NewFundingLine(_ => _.WithCalculations(new[] { NewFundingLineCalculation(_ => _.WithId(1)
                .WithCalculationNamespaceType(CalculationNamespace.Template))})
                .WithId(2)
                .WithName("One")
                .WithSourceCodeName("One")
                .WithNamespace("PSG"));

            IDictionary<string, Funding> fundingLines = new Dictionary<string, Funding> {
                {"1619", NewFunding(_ => _.WithFundingLines(new[] { _1619fl }))},
                {"PSG", NewFunding(_ => _.WithFundingLines(new[] { psgfl }))}
            };

            IEnumerable<SourceFile> results = new CalculationTypeGenerator(new CompilerOptions()).GenerateCalcs(new[] { _1619, psg }, fundingLines);

            results.Should().HaveCount(1);
            results.First()
                .SourceCode
                .Should()
                .ContainAll("Public Class PSGCalculations",
                    "Public Class _1619Calculations",
                    "Public Class AdditionalCalculations",
                    "Public Class PSGFundingLines",
                    "Public Class _1619FundingLines");
        }

        [TestMethod]
        public void GenerateCalculations_GivenNoAdditionalCalculations_ThenFundingLineMembersCreated()
        {
            Calculation _1619 = NewCalculation(_ => _.WithFundingStream(NewReference(rf => rf.WithId("DSG")))
                .WithSourceCodeName("One")
                .WithCalculationNamespaceType(CalculationNamespace.Template)
                .WithSourceCode("return 456"));
            Calculation psg = NewCalculation(_ => _.WithFundingStream(NewReference(rf => rf.WithId("PSG")))
                .WithCalculationNamespaceType(CalculationNamespace.Template)
                .WithSourceCodeName("Two")
                .WithSourceCode("return _1619.One() + 100"));

            FundingLine _1619fl = NewFundingLine(_ => _.WithCalculations(new[] { NewFundingLineCalculation(_ => _.WithId(1)
                .WithCalculationNamespaceType(CalculationNamespace.Template))})
                .WithId(1)
                .WithName("One")
                .WithSourceCodeName("One")
                .WithNamespace("1619"));

            FundingLine psgfl = NewFundingLine(_ => _.WithCalculations(new[] { NewFundingLineCalculation(_ => _.WithId(1)
                .WithCalculationNamespaceType(CalculationNamespace.Template))})
                .WithId(2)
                .WithName("Two")
                .WithSourceCodeName("Two")
                .WithNamespace("PSG"));

            IDictionary<string, Funding> fundingLines = new Dictionary<string, Funding> {
                {"1619", NewFunding(_ => _.WithFundingLines(new[] { _1619fl }))},
                {"PSG", NewFunding(_ => _.WithFundingLines(new[] { psgfl }))}
            };

            IEnumerable<SourceFile> results = new CalculationTypeGenerator(new CompilerOptions()).GenerateCalcs(new[] { _1619, psg }, fundingLines);

            results.Should().HaveCount(1);
            results.First()
                .SourceCode
                .Should()
                .ContainAll(
                    "<FundingLine(FundingStream:=\"1619\", Id:=\"1\", Name:=\"One\")>",
                    "Public One As Func(Of decimal?) = Nothing",
                    "<FundingLine(FundingStream:=\"PSG\", Id:=\"2\", Name:=\"Two\")>",
                    "Public Two As Func(Of decimal?) = Nothing",
                    "_1619.FundingLines.One()");
        }

        [TestMethod]
        public void GenerateCalculations_GiveEnumCalculationType()
        {
            Calculation _1619 = NewCalculation(_ => _.WithFundingStream(NewReference(rf => rf.WithId("1619")))
            .WithName("MethodologyType")
            .WithValueType(CalculationValueType.String)
            .WithDataType(CalculationDataType.Enum)
            .WithAllowedEnumTypeValues(new[] { "Type1", "Type2", "Type3" })
               .WithSourceCodeName("One")
               .WithCalculationNamespaceType(CalculationNamespace.Template)
               .WithSourceCode("return Nothing"));

            FundingLine _1619fl = NewFundingLine(_ => _.WithCalculations(new[] { NewFundingLineCalculation(_ => _.WithId(1)
                .WithCalculationNamespaceType(CalculationNamespace.Template))})
                .WithId(2)
                .WithName("Two")
                .WithSourceCodeName("Two")
                .WithNamespace("1619"));

            IDictionary<string, Funding> fundingLines = new Dictionary<string, Funding> {
                {"1619", NewFunding(_ => _.WithFundingLines(new[] { _1619fl }))}
            };

            IEnumerable<SourceFile> results = new CalculationTypeGenerator(new CompilerOptions()).GenerateCalcs(new[] { _1619 }, fundingLines).ToList();

            results.Should().HaveCount(1);

            results.First().SourceCode.Should().Contain("Public Enum MethodologyTypeOptions");
            results.First().SourceCode.Should().Contain("Public One As Func(Of MethodologyTypeOptions?) = Nothing");
            results.First().SourceCode.Should().Contain("Return If(String.IsNullOrWhiteSpace(existingCalculationResultString), Nothing, CType([Enum].Parse(GetType(MethodologyTypeOptions), existingCalculationResultString), MethodologyTypeOptions?))");
            results.First().SourceCode.Should().Contain($"calculationContext.DictionaryStringValues.Add(\"{_1619.Id}\", executedUserCodeCalculationResult?.ToString())");
        }

        [TestMethod]
        public void GenerateCalculations_GivenSpaceInEnumReturnsValidEnumCalculationType()
        {
            Calculation _1619 = NewCalculation(_ => _.WithFundingStream(NewReference(rf => rf.WithId("1619")))
            .WithName("MethodologyType")
            .WithValueType(CalculationValueType.String)
            .WithDataType(CalculationDataType.Enum)
            .WithAllowedEnumTypeValues(new[] { "Type1 ", "Type2", "Type3" })
               .WithSourceCodeName("One")
               .WithCalculationNamespaceType(CalculationNamespace.Template)
               .WithSourceCode("return Nothing"));

            FundingLine _1619fl = NewFundingLine(_ => _.WithCalculations(new[] { NewFundingLineCalculation(_ => _.WithId(1)
                .WithCalculationNamespaceType(CalculationNamespace.Template))})
                .WithId(2)
                .WithName("Two")
                .WithSourceCodeName("Two")
                .WithNamespace("1619"));

            IDictionary<string, Funding> fundingLines = new Dictionary<string, Funding> {
                {"1619", NewFunding(_ => _.WithFundingLines(new[] { _1619fl }))}
            };

            IEnumerable<SourceFile> results = new CalculationTypeGenerator(new CompilerOptions()).GenerateCalcs(new[] { _1619 }, fundingLines).ToList();

            results.Should().HaveCount(1);

            results.First().SourceCode.Should().Contain("Enum MethodologyTypeOptions");
            results.First().SourceCode.Should().Contain("Public One As Func(Of MethodologyTypeOptions?) = Nothing");
        }

        [TestMethod]
        public void GenerateCalculations_GivenNoCalculations_ThenSingleInnerClassForAdditionalCreated()
        {
            Dictionary<string, Funding> fundingLines = new Dictionary<string, Funding>();

            IEnumerable<SourceFile> results = new CalculationTypeGenerator(new CompilerOptions()).GenerateCalcs(Enumerable.Empty<Calculation>(), fundingLines);

            results.Should().HaveCount(1);
            results.First()
                .SourceCode
                .Should()
                .ContainAll("Public Class AdditionalCalculations");
        }

        [TestMethod]
        public void GenerateCalculations_GivenCalculationsInDifferentNamespaces_ThenInnerClassPerNamespaceCreated()
        {
            Calculation _1619 = NewCalculation(_ => _.WithFundingStream(NewReference(rf => rf.WithId("1619")))
                .WithSourceCodeName("One")
                .WithCalculationNamespaceType(CalculationNamespace.Template)
                .WithSourceCode("return 456"));
            Calculation psg = NewCalculation(_ => _.WithFundingStream(NewReference(rf => rf.WithId("PSG")))
                .WithCalculationNamespaceType(CalculationNamespace.Template)
                .WithSourceCodeName("One")
                .WithSourceCode("return Calculations.One() + 100"));
            Calculation additionalOne = NewCalculation(_ => _.WithFundingStream(NewReference())
                .WithCalculationNamespaceType(CalculationNamespace.Additional)
                .WithSourceCodeName("One")
                .WithSourceCode("return DSG.One() + Calculations.Two() + 34"));
            Calculation additionalTwo = NewCalculation(_ => _.WithFundingStream(NewReference())
                .WithCalculationNamespaceType(CalculationNamespace.Additional)
                .WithSourceCodeName("Two")
                .WithSourceCode("return 789"));

            Dictionary<string, Funding> fundingLines = new Dictionary<string, Funding>();

            IEnumerable<SourceFile> results = new CalculationTypeGenerator(new CompilerOptions()).GenerateCalcs(new[] { _1619, psg, additionalOne, additionalTwo }, fundingLines);

            results.Should().HaveCount(1);
            results.First()
                .SourceCode
                .Should()
                .ContainAll("Public Class PSGCalculations",
                    "Public Class _1619Calculations",
                    "Public Class AdditionalCalculations");
        }

        [TestMethod]
        [DataRow(false, "Inherits BaseCalculation")]
        [DataRow(true, "Inherits LegacyBaseCalculation")]
        public void GenerateCalcs_GivenCalculationsAndCompilerOptionsUseLegacyCodeIsSet_TheEnsuresCorrectInheritanceStatement(bool useLegacyCode, string expectedInheritsStatement)
        {
            // Arrange
            CompilerOptions compilerOptions = new CompilerOptions
            {
                UseLegacyCode = useLegacyCode,
            };

            CalculationTypeGenerator calculationTypeGenerator = new CalculationTypeGenerator(compilerOptions);
            Dictionary<string, Funding> fundingLines = new Dictionary<string, Funding>();

            // Act
            IEnumerable<SourceFile> results = calculationTypeGenerator.GenerateCalcs(new List<Calculation>(), fundingLines);

            // Assert
            results.Should().HaveCount(1);

            results.First().SourceCode.Should().Contain(expectedInheritsStatement);
        }

        [TestMethod]
        public void GenerateCalcs_InvalidSourceCodeNormaliseWhitespaceFails_ReturnsError()
        {
            CompilerOptions compilerOptions = new CompilerOptions();
            CalculationTypeGenerator calculationTypeGenerator = new CalculationTypeGenerator(compilerOptions);

            string badCode = @"Dim Filter as Decimal
Dim APTPhase as String
Dim CensusPhase as String
Dim Result as Decimal

Filter = FILTERSBSAcademiesFilter()
APTPhase = Datasets.APTInputsAndAdjustments.Phase()
CensusPhase = Datasets.CensusPupilCharacteristics.Phase()

If Filter = 0 then

Return Exclude()

Else

If string.isnullorempty(APTPhase) then Result = 0

Else If CensusPhase = ""PRIMARY"" then Result = 1

Else If CensusPhase = ""MIDDLE-DEEMED PRIMARY"" then Result = 2

Else If CensusPhase = ""SECONDARY"" then Result = 3

Else If CensusPhase = ""MIDDLE-DEEMED SECONDARY"" then Result = 4

End If
End If
End If

Return Result + 0";

            IEnumerable<Calculation> calculations = new[] { new Calculation { Current = new CalculationVersion { SourceCode = badCode, SourceCodeName = "Broken" } } };

            Dictionary<string, Funding> fundingLines = new Dictionary<string, Funding>();

            Action generate = () => calculationTypeGenerator.GenerateCalcs(calculations, fundingLines).ToList();

            generate
                .Should()
                .Throw<Exception>()
                .And.Message
                .Should()
                .StartWith("Error compiling source code. Please check your code's structure is valid. ");
        }

        [TestMethod]
        public void GenerateCalcs_MissingSourceCodeName_ReturnsError()
        {
            string id = "42";

            CompilerOptions compilerOptions = new CompilerOptions();
            CalculationTypeGenerator calculationTypeGenerator = new CalculationTypeGenerator(compilerOptions);

            IEnumerable<Calculation> calculations = new[] { new Calculation {
                Current = new CalculationVersion { SourceCode = "Return 1", Namespace = CalculationNamespace.Additional}, Id = id } };

            Dictionary<string, Funding> fundingLines = new Dictionary<string, Funding>();

            Action generate = () => calculationTypeGenerator.GenerateCalcs(calculations, fundingLines).ToList();

            generate
                .Should()
                .Throw<Exception>()
                .And.Message
                .Should()
                .Be($"Calculation source code name is not populated for calc {id}");
        }

        private FundingLineCalculation NewFundingLineCalculation(Action<FundingLineCalculationBuilder> setUp = null)
        {
            FundingLineCalculationBuilder calculationBundingLineBuilder = new FundingLineCalculationBuilder();

            setUp?.Invoke(calculationBundingLineBuilder);

            return calculationBundingLineBuilder.Build();
        }

        private Funding NewFunding(Action<FundingBuilder> setUp = null)
        {
            FundingBuilder fundingBuilder = new FundingBuilder();

            setUp?.Invoke(fundingBuilder);

            return fundingBuilder.Build();
        }

        private FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        private Calculation NewCalculation(Action<CalculationBuilder> setUp = null)
        {
            CalculationBuilder calculationBuilder = new CalculationBuilder();

            setUp?.Invoke(calculationBuilder);

            return calculationBuilder.Build();
        }

        private Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }
    }
}
