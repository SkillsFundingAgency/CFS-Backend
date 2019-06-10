using System;
using System.Collections.Generic;
using System.Linq;
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
        public void WhenLessThanSymbolInName_ThenIdentifierIsSubstituted()
        {
            // Arrange
            string inputName = "Range < 3";

            // Act
            string result = CalculationTypeGenerator.GenerateIdentifier(inputName);

            // Assert
            result
                .Should()
                .Be("RangeLessThan3");
        }

        [TestMethod]
        public void WhenGreaterThanSymbolInName_ThenIdentifierIsSubstituted()
        {
            // Arrange
            string inputName = "Range > 3";

            // Act
            string result = CalculationTypeGenerator.GenerateIdentifier(inputName);

            // Assert
            result
                .Should()
                .Be("RangeGreaterThan3");
        }

        [TestMethod]
        public void WhenPoundSymbolInName_ThenIdentifierIsSubstituted()
        {
            // Arrange
            string inputName = "Range £ 3";

            // Act
            string result = CalculationTypeGenerator.GenerateIdentifier(inputName);

            // Assert
            result
                .Should()
                .Be("RangePound3");
        }

        [TestMethod]
        public void WhenPercentSymbolInName_ThenIdentifierIsSubstituted()
        {
            // Arrange
            string inputName = "Range % 3";

            // Act
            string result = CalculationTypeGenerator.GenerateIdentifier(inputName);

            // Assert
            result
                .Should()
                .Be("RangePercent3");
        }

        [TestMethod]
        public void WhenEqualsSymbolInName_ThenIdentifierIsSubstituted()
        {
            // Arrange
            string inputName = "Range = 3";

            // Act
            string result = CalculationTypeGenerator.GenerateIdentifier(inputName);

            // Assert
            result
                .Should()
                .Be("RangeEquals3");
        }

        [TestMethod]
        public void WhenPlusSymbolInName_ThenIdentifierIsSubstituted()
        {
            // Arrange
            string inputName = "Range + 3";

            // Act
            string result = CalculationTypeGenerator.GenerateIdentifier(inputName);

            // Assert
            result
                .Should()
                .Be("RangePlus3");
        }

        [TestMethod]
        public void QuoteAggregateFunctionCalls_GivenNoAggregateCalls_DoesNotQuoteParameter()
        {
            //Arrange
            string sourceCode = "return calc1()";

            string expected = sourceCode;

            //Act
            string result = CalculationTypeGenerator.QuoteAggregateFunctionCalls(sourceCode);

            //Assert
            result
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void QuoteAggregateFunctionCalls_GivenAggregateAvgCall_QuotesParameter()
        {
            //Arrange
            string sourceCode = "return Avg(calc1)";
            string expected = "return Avg(\"calc1\")";

            //Act
            string result = CalculationTypeGenerator.QuoteAggregateFunctionCalls(sourceCode);

            //Assert
            result
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void QuoteAggregateFunctionCalls_GivenAggregateMaxCall_QuotesParameter()
        {
            //Arrange
            string sourceCode = "return Max(calc1)";
            string expected = "return Max(\"calc1\")";

            //Act
            string result = CalculationTypeGenerator.QuoteAggregateFunctionCalls(sourceCode);

            //Assert
            result
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void QuoteAggregateFunctionCalls_GivenAggregateMinCall_QuotesParameter()
        {
            //Arrange
            string sourceCode = "return Min(calc1)";
            string expected = "return Min(\"calc1\")";

            //Act
            string result = CalculationTypeGenerator.QuoteAggregateFunctionCalls(sourceCode);

            //Assert
            result
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void QuoteAggregateFunctionCalls_GivenAggregateSumCall_QuotesParameter()
        {
            //Arrange
            string sourceCode = "return Sum(calc1)";
            string expected = "return Sum(\"calc1\")";

            //Act
            string result = CalculationTypeGenerator.QuoteAggregateFunctionCalls(sourceCode);

            //Assert
            result
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void QuoteAggregateFunctionCalls_GivenAggregateSumCall_QuotesParameterIgnoringWhitespace()
        {
            //Arrange
            string sourceCode = "return    Max(calc1)";
            string expected = "return    Max(\"calc1\")";

            //Act
            string result = CalculationTypeGenerator.QuoteAggregateFunctionCalls(sourceCode);

            //Assert
            result
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void QuoteAggregateFunctionCalls_GivenCalculationReferenceContainsAvgButNotAnAggregateCall_DoesNotQuoteParameter()
        {
            //Arrange
            string sourceCode = "return AvgTest()";
            string expected = "return AvgTest()";

            //Act
            string result = CalculationTypeGenerator.QuoteAggregateFunctionCalls(sourceCode);

            //Assert
            result
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void QuoteAggregateFunctionCalls_GivenCalculationReferenceContainsSumButNotAnAggregateCall_DoesNotQuoteParameter()
        {
            //Arrange
            string sourceCode = "return SumTest()";
            string expected = "return SumTest()";

            //Act
            string result = CalculationTypeGenerator.QuoteAggregateFunctionCalls(sourceCode);

            //Assert
            result
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void QuoteAggregateFunctionCalls_GivenCalculationReferenceContainsMinButNotAnAggregateCall_DoesNotQuoteParameter()
        {
            //Arrange
            string sourceCode = "return TestMinTest()";
            string expected = "return TestMinTest()";

            //Act
            string result = CalculationTypeGenerator.QuoteAggregateFunctionCalls(sourceCode);

            //Assert
            result
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void QuoteAggregateFunctionCalls_GivenCalculationReferenceContainsAvgButNotAAggregateCallButDoesContainMaxAggregateCall_QuotesCorrectParameter()
        {
            //Arrange
            string sourceCode = "return TestSum() + Max(calc1)";
            string expected = "return TestSum() + Max(\"calc1\")";

            //Act
            string result = CalculationTypeGenerator.QuoteAggregateFunctionCalls(sourceCode);

            //Assert
            result
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void QuoteAggregateFunctionCalls_GivenAggregateSumAndMaxCall_QuotesParameterIgnoringWhitespace()
        {
            //Arrange
            string sourceCode = "return    Max(calc1) + Sum(calc1)";
            string expected = "return    Max(\"calc1\") + Sum(\"calc1\")";

            //Act
            string result = CalculationTypeGenerator.QuoteAggregateFunctionCalls(sourceCode);

            //Assert
            result
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void QuoteAggregateFunctionCalls_GivenAggregateSumAndMaxCallWithSpacesInsideParentheses_QuotesParameterIgnoringWhitespace()
        {
            //Arrange
            string sourceCode = "return    Max( calc1     ) + Sum(    calc1    )";
            string expected = "return    Max(\"calc1\") + Sum(\"calc1\")";

            //Act
            string result = CalculationTypeGenerator.QuoteAggregateFunctionCalls(sourceCode);

            //Assert
            result
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void QuoteAggregateFunctionCalls_GivenNonAggregateCallWithSpacesInsideParentheses_QuotesParameterIgnoringWhitespace()
        {
            //Arrange
            string sourceCode = "If(InpAdjPupilNumberGuaranteed= \"YES\" and InputAdjNOR > NOREstRtoY11) then";
            string expected = "If(InpAdjPupilNumberGuaranteed= \"YES\" and InputAdjNOR > NOREstRtoY11) then";

            //Act
            string result = CalculationTypeGenerator.QuoteAggregateFunctionCalls(sourceCode);

            //Assert
            result
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void QuoteAggregateFunctionCalls_GivenCalculationIsUsingMathLibrarytheses_EnsuresThatIsIgnoredAndDoesNotTreatAsAggregateFunctions()
        {
            //Arrange
            string sourceCode = "FundingRate1 = -1 * (Math.Min(ThresholdZ, FundingRateZ) / FundingRateZ) * Condition1 + Sum(test1)";
            string expected = "FundingRate1 = -1 * (Math.Min(ThresholdZ, FundingRateZ) / FundingRateZ) * Condition1 + Sum(\"test1\")";

            //Act
            string result = CalculationTypeGenerator.QuoteAggregateFunctionCalls(sourceCode);

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

            CompilerOptions compilerOptions = new CompilerOptions
            {

                OptionStrictEnabled = true
            };

            CalculationTypeGenerator calculationTypeGenerator = new CalculationTypeGenerator(compilerOptions, true);

            // Act
            IEnumerable<SourceFile> results = calculationTypeGenerator.GenerateCalcs(calculations);

            // Assert
            results.Should().HaveCount(1);
            results.First().SourceCode.Should().StartWith("Option Strict On");
        }

        [TestMethod]
        public void GenerateCalcs_GivenCalculationsAndCompilerOptionsOff_ThenOptionsGenerated()
        {
            // Arrange
            List<Calculation> calculations = new List<Calculation>();

            CompilerOptions compilerOptions = new CompilerOptions
            {
                OptionStrictEnabled = false
            };

            CalculationTypeGenerator calculationTypeGenerator = new CalculationTypeGenerator(compilerOptions, true);

            // Act
            IEnumerable<SourceFile> results = calculationTypeGenerator.GenerateCalcs(calculations);

            // Assert
            results.Should().HaveCount(1);
            results.First().SourceCode.Should().StartWith("Option Strict Off");
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

            CalculationTypeGenerator calculationTypeGenerator = new CalculationTypeGenerator(compilerOptions, true);

            // Act
            IEnumerable<SourceFile> results = calculationTypeGenerator.GenerateCalcs(new List<Calculation>());

            // Assert
            results.Should().HaveCount(1);

            results.First().SourceCode.Should().Contain(expectedInheritsStatement);
        }

        [TestMethod]
        public void GenerateCalcs_InvalidSourceCodeNormaliseWhitespaceFails_ReturnsError()
        {
            CompilerOptions compilerOptions = new CompilerOptions();
            CalculationTypeGenerator calculationTypeGenerator = new CalculationTypeGenerator(compilerOptions, true);

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

            IEnumerable<Calculation> calculations = new[] { new Calculation { Current = new CalculationVersion { SourceCode = badCode } } };

            Action generate = () => calculationTypeGenerator.GenerateCalcs(calculations).ToList();

            generate
                .Should()
                .Throw<Exception>()
                .And.Message
                .Should()
                .StartWith("Error compiling source code. Please check your code's structure is valid. ");
        }

        [TestMethod]
        [DataRow("Nullable(Of Decimal)")]
        [DataRow("Nullable(Of Integer)")]
        public void GenerateIdentifier_WhenSymbolIsNullableType_DoesNotSubstitute(string input)
        {
            // Arrange
            string inputName = input;

            // Act
            string result = CalculationTypeGenerator.GenerateIdentifier(inputName);

            // Assert
            result
                .Should()
                .Be(input);
        }
    }
}
