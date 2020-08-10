using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Core.Extensions;
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

            GivenTheFundingLine(psg, NewFunding(_ => _.WithFundingLines(new[] { NewFundingLine(fl => fl.WithCalculations(new[] { NewFundingLineCalculation(_ => _.WithId(1)
                .WithCalculationNamespaceType(CalculationNamespace.Template))})
                .WithId(1)
                .WithName("One")
                .WithSourceCodeName("One")
                .WithNamespace(psg)) })));

            AndTheFundingLine(dsg, NewFunding(_ => _.WithFundingLines(new[] { NewFundingLine(fl => fl.WithCalculations(new[] { NewFundingLineCalculation(_ => _.WithId(1)
                .WithCalculationNamespaceType(CalculationNamespace.Template))})
                .WithId(2)
                .WithName("Two")
                .WithSourceCodeName("Two")
                .WithNamespace(dsg)) })));

            WhenTheFundingLineNamespacesAreBuilt();

            ThenResultContainsNamespaceDefinitionsFor(psg, dsg);
        }

        [TestMethod]
        public void EachNamespaceClassContainsAMethodToInitialiseAllOfTheSharedContextAndNamespaces()
        {
            string psg = "PSG";

            string[] expectedInitialiseMethod = {
                @"PSGFundingLines<FundingLine(FundingStream := ""PSG"", Id := ""1"", Name := ""One"")>
Public One As Func(Of decimal?) = Nothing
Public Property PSG As PSGCalculationsPublic Sub Initialise(calculationContext As CalculationContext)
PSG = calculationContext.PSG

One = Function() As Decimal?"};

            GivenTheFundingLine(psg, NewFunding(_ => _.WithFundingLines(new[] { NewFundingLine(fl => fl.WithCalculations(new[] { NewFundingLineCalculation(_ => _.WithId(1)
                .WithCalculationNamespaceType(CalculationNamespace.Template))})
                .WithId(1)
                .WithName("One")
                .WithSourceCodeName("One")
                .WithNamespace(psg)) })));

            WhenTheFundingLineNamespacesAreBuilt();

            ThenResultContainsNamespaceDefinitionsFor(psg);
            AndTheNamespaceDefinition(psg, ns => HasSourceCodeContaining(ns, expectedInitialiseMethod));
        }

        [TestMethod]
        public void EachNamespaceClassContainsPublicFieldsToAccessItsFundingLineResultssAsFunctionPointers()
        {
            string psg = "PSG";
            string dsg = "DSG";

            string[] expectedFunctionPointers = {
                "Public One As Func(Of decimal?) = Nothing"
            };

            GivenTheFundingLine(psg, NewFunding(_ => _.WithFundingLines(new[] { NewFundingLine(fl => fl.WithCalculations(new[] { NewFundingLineCalculation(_ => _.WithId(1)
                .WithCalculationNamespaceType(CalculationNamespace.Template))})
                .WithId(1)
                .WithName("One")
                .WithSourceCodeName("One")
                .WithNamespace(psg)) })));

            GivenTheFundingLine(dsg, NewFunding(_ => _.WithFundingLines(new[] { NewFundingLine(fl => fl.WithCalculations(new[] { NewFundingLineCalculation(_ => _.WithId(1)
                .WithCalculationNamespaceType(CalculationNamespace.Template))})
                .WithId(2)
                .WithName("Two")
                .WithSourceCodeName("Two")
                .WithNamespace(dsg)) })));

            WhenTheFundingLineNamespacesAreBuilt();

            ThenResultContainsNamespaceDefinitionsFor(psg, dsg);
            AndTheNamespaceDefinition(psg, ns => HasSourceCodeContaining(ns, expectedFunctionPointers));
        }

        private void GivenTheFundingLine(string fundingStream, Funding funding)
        {
            _fundingLines.Add(fundingStream, funding);
        }

        private void AndTheFundingLine(string fundingStream, Funding funding)
        {
            GivenTheFundingLine(fundingStream, funding);
        }

        private void WhenTheFundingLineNamespacesAreBuilt()
        {
            _result = _builder.BuildNamespacesForFundingLines(_fundingLines);
        }

        private void ThenResultContainsNamespaceDefinitionsFor(params string[] expectedNamespaces)
        {
            _result.InnerClasses
                .Select(_ => _.Namespace)
                .Should()
                .BeEquivalentTo(expectedNamespaces);
        }

        private void AndTheNamespaceDefinition(string @namespace, Func<NamespaceClassDefinition, bool> predicate)
        {
            NamespaceClassDefinition namespaceDefinition = _result.InnerClasses.FirstOrDefault(_ => _.Namespace == @namespace);

            namespaceDefinition.Should()
                .NotBeNull();

            predicate(namespaceDefinition)
                .Should()
                .BeTrue();
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

        private Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        private bool HasSourceCodeContaining(NamespaceClassDefinition namespaceClassDefinition,
            params string[] expectedSourceCode)
        {
            var classSourceCode = GetSourceForClassBlock(namespaceClassDefinition.ClassBlockSyntax);

            return expectedSourceCode.All(expectedSourceCodeSnippet
                => classSourceCode.Contains(expectedSourceCodeSnippet));
        }

        private bool HasSourceCodeNotContaining(NamespaceClassDefinition namespaceClassDefinition,
            params string[] expectedSourceCode)
        {
            return !HasSourceCodeContaining(namespaceClassDefinition, expectedSourceCode);
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