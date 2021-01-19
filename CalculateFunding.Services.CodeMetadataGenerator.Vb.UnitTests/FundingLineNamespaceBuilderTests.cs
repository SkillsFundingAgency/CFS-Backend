using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.CodeMetadataGenerator.Vb.UnitTests
{
    [TestClass]
    public class FundingLineNamespaceBuilderTests
    {
        private IDictionary<string, Funding> _fundingLines;

        private FundingLineNamespaceBuilder _builder;

        private NamespaceBuilderResult _result;

        [TestInitialize]
        public void SetUp()
        {
            _fundingLines = new Dictionary<string, Funding>();
            _builder = new FundingLineNamespaceBuilder();
        }

        [TestMethod]
        public void CreatesInnerClassForEachNamespaceInSpecification()
        {
            string psg = "PSG";
            string dsg = "DSG";

            GivenTheFundingLine(psg,
                NewFunding(_ => _.WithFundingLines(NewFundingLine(fl => fl.WithCalculations(NewFundingLineCalculation(_ => _.WithId(1)
                        .WithCalculationNamespaceType(CalculationNamespace.Template)))
                    .WithId(1)
                    .WithName("One")
                    .WithSourceCodeName("One")
                    .WithNamespace(psg)))));

            AndTheFundingLine(dsg,
                NewFunding(_ => _.WithFundingLines(NewFundingLine(fl => fl.WithCalculations(NewFundingLineCalculation(_ => _.WithId(1)
                        .WithCalculationNamespaceType(CalculationNamespace.Template)))
                    .WithId(2)
                    .WithName("Two")
                    .WithSourceCodeName("Two")
                    .WithNamespace(dsg)))));

            WhenTheFundingLineNamespacesAreBuilt();

            ThenResultContainsNamespaceDefinitionsFor(psg, dsg);
        }

        [TestMethod]
        public void EachNamespaceClassContainsAMethodToInitialiseAllOfTheSharedContextAndNamespaces()
        {
            string psg = "PSG";

            string[] expectedInitialiseMethod =
            {
                @"PSGFundingLines<FundingLine(FundingStream := ""PSG"", Id := ""1"", Name := ""One"")>
Public One As Func(Of decimal?) = Nothing
Public Property PSG As PSGCalculations
Sub AddToNullable(ByRef sum As Decimal?, amount as Decimal?)
    If sum.HasValue Then
        sum = amount.GetValueOrDefault() + sum
    Else
        sum = amount
    End If
End Sub
Public Sub Initialise(calculationContext As CalculationContext)
PSG = calculationContext.PSG

One = Function() As Decimal?"
            };

            GivenTheFundingLine(psg,
                NewFunding(_ => _
                    .WithFundingLines(NewFundingLine(fl => fl
                        .WithCalculations(NewFundingLineCalculation(cal => cal.WithId(1)
                            .WithCalculationNamespaceType(CalculationNamespace.Template)))
                        .WithId(1)
                        .WithName("One")
                        .WithSourceCodeName("One")
                        .WithNamespace(psg)))));

            WhenTheFundingLineNamespacesAreBuilt();

            ThenResultContainsNamespaceDefinitionsFor(psg);
            AndTheNamespaceDefinition(psg, expectedInitialiseMethod);
        }

        [TestMethod]
        public void EachFundingLineFunctionRoundsTheSumToTheSuppliedDecimalPlaces()
        {
            int decimalPlaces = new RandomNumberBetween(2, 99);

            string psg = "PSG";

            string expectedFundingLineLambda = @$"Dim userCalculationCodeImplementation As Func(Of Decimal?) = Function() As Decimal?
Dim sum As Decimal? = Nothing
AddToNullable(sum, Template.CalcOne())
Return If(sum.HasValue(), Math.Round(sum.Value, {decimalPlaces}, MidpointRounding.AwayFromZero), sum)
End Function";

            GivenTheFundingLine(psg,
                NewFunding(_ => _
                    .WithFundingLines(NewFundingLine(fl => fl
                        .WithCalculations(NewFundingLineCalculation(cal => cal
                            .WithId(1)
                            .WithName("CalcOne")
                            .WithSourceCodeName("CalcOne")
                            .WithCalculationNamespaceType(CalculationNamespace.Template)))
                        .WithId(1)
                        .WithNamespace(psg)))));

            WhenTheFundingLineNamespacesAreBuilt(decimalPlaces);

            AndTheNamespaceDefinition(psg, expectedFundingLineLambda);
        }

        [TestMethod]
        public void EachNamespaceClassContainsPublicFieldsToAccessItsFundingLineResultsAsFunctionPointers()
        {
            string psg = "PSG";
            string dsg = "DSG";

            string[] expectedFunctionPointers =
            {
                "Public One As Func(Of decimal?) = Nothing"
            };

            GivenTheFundingLine(psg,
                NewFunding(_ => _
                    .WithFundingLines(NewFundingLine(fl => fl
                        .WithCalculations(NewFundingLineCalculation(cal => cal
                            .WithId(1)
                            .WithCalculationNamespaceType(CalculationNamespace.Template)))
                        .WithId(1)
                        .WithName("One")
                        .WithSourceCodeName("One")
                        .WithNamespace(psg)))));

            GivenTheFundingLine(dsg,
                NewFunding(_ => _
                    .WithFundingLines(NewFundingLine(fl => fl
                        .WithCalculations(NewFundingLineCalculation(cal => cal
                            .WithId(1)
                            .WithCalculationNamespaceType(CalculationNamespace.Template)))
                        .WithId(2)
                        .WithName("Two")
                        .WithSourceCodeName("Two")
                        .WithNamespace(dsg)))));

            WhenTheFundingLineNamespacesAreBuilt();

            ThenResultContainsNamespaceDefinitionsFor(psg, dsg);
            AndTheNamespaceDefinition(psg, expectedFunctionPointers);
        }

        private void GivenTheFundingLine(string fundingStream,
            Funding funding)
        {
            _fundingLines.Add(fundingStream, funding);
        }

        private void AndTheFundingLine(string fundingStream,
            Funding funding)
        {
            GivenTheFundingLine(fundingStream, funding);
        }

        private void WhenTheFundingLineNamespacesAreBuilt(int decimalPlaces = 2)
        {
            _result = _builder.BuildNamespacesForFundingLines(_fundingLines, decimalPlaces);
        }

        private void ThenResultContainsNamespaceDefinitionsFor(params string[] expectedNamespaces)
        {
            _result.InnerClasses
                .Select(_ => _.Namespace)
                .Should()
                .BeEquivalentTo(expectedNamespaces);
        }

        private void AndTheNamespaceDefinition(string @namespace,
            params string[] expectedSource)
        {
            NamespaceClassDefinition namespaceDefinition = _result.InnerClasses.FirstOrDefault(_ => _.Namespace == @namespace);

            namespaceDefinition.Should()
                .NotBeNull();

            string sourceCode = GetSourceForClassBlock(namespaceDefinition.ClassBlockSyntax);

            foreach (string block in expectedSource)
            {
                sourceCode
                    .Should()
                    .Contain(block);
            }
        }

        private FundingLineCalculation NewFundingLineCalculation(Action<FundingLineCalculationBuilder> setUp = null)
        {
            FundingLineCalculationBuilder fundingLineCalculationBuilder = new FundingLineCalculationBuilder();

            setUp?.Invoke(fundingLineCalculationBuilder);

            return fundingLineCalculationBuilder.Build();
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

        private bool HasSourceCodeContaining(NamespaceClassDefinition namespaceClassDefinition,
            params string[] expectedSourceCode)
        {
            string classSourceCode = GetSourceForClassBlock(namespaceClassDefinition.ClassBlockSyntax);

            return expectedSourceCode.All(expectedSourceCodeSnippet
                => classSourceCode.Contains(expectedSourceCodeSnippet));
        }

        private string GetSourceForClassBlock(ClassBlockSyntax classBlockSyntax)
        {
            CompilationUnitSyntax syntaxTree = SyntaxFactory
                .CompilationUnit()
                .WithOptions(new SyntaxList<OptionStatementSyntax>(new[]
                {
                    SyntaxFactory.OptionStatement(SyntaxFactory.Token(SyntaxKind.StrictKeyword),
                        SyntaxFactory.Token(SyntaxKind.OnKeyword))
                }))
                .WithImports(StandardImports())
                .WithMembers(SyntaxFactory.SingletonList<StatementSyntax>(classBlockSyntax));

            return syntaxTree.ToFullString();
        }

        private static SyntaxList<ImportsStatementSyntax> StandardImports()
        {
            return SyntaxFactory.List(new[]
            {
                SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(
                    SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("System")))),
                SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(
                    SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("System.Collections.Generic")))),
                SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(
                    SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("Microsoft.VisualBasic.CompilerServices"))))
            });
        }
    }
}