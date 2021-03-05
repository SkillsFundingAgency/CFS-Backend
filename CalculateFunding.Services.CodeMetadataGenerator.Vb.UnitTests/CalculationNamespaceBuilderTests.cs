using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.CodeMetadataGenerator.Vb.UnitTests
{
    [TestClass]
    public class CalculationNamespaceBuilderTests
    {
        private const string Additional = "Calculations";

        private ICollection<ObsoleteItem> _obsoleteItems;
        private Dictionary<string, Funding> _funding;
        private List<Calculation> _calculations;
        private CalculationNamespaceBuilder _builder;

        private NamespaceBuilderResult _result;

        [TestInitialize]
        public void SetUp()
        {
            _obsoleteItems = new List<ObsoleteItem>();
            _funding = new Dictionary<string, Funding>();
            _calculations = new List<Calculation>();
            _builder = new CalculationNamespaceBuilder(new CompilerOptions
            {
                OptionStrictEnabled = true
            });
        }

        [TestMethod]
        public void CreatesInnerClassForEachNamespaceInSpecification()
        {
            const string psg = "PSG";
            const string _1619 = "1619";

            GivenTheCalculations(NewCalculationWithNamespace(psg),
                NewCalculationWithNamespace(_1619));

            WhenTheCalculationNamespacesAreBuilt();

            ThenResultContainsNamespaceDefinitionsFor(psg, $"_{_1619}", Additional);
        }

        [TestMethod]
        public void CreatesEnumForEachEnumCalculationInSpecification()
        {
            const string EnumName = nameof(EnumName);
            const string Option1 = nameof(Option1);
            const string Option2 = nameof(Option2);

            string expectedEnumSourceCode =
                @$"Enum {EnumName}Options
    <Description(Description:=""{Option1}"")>
    {Option1}
    <Description(Description:=""{Option2}"")>
    {Option2}
End Enum";

            GivenTheCalculations(NewCalculationWithNamespaceAndEnumDataType(EnumName,
                new[]
                {
                    Option1,
                    Option2
                }));

            WhenTheCalculationNamespacesAreBuilt();

            ThenTheEnumDefinition(_ => StatementHasSourceCodeContaining(_, expectedEnumSourceCode));
        }

        [TestMethod]
        public void StubsEnumEntriesForObsoleteEnumItemsForTheSpecification()
        {
            const string EnumName = nameof(EnumName);
            const string Option1 = nameof(Option1);
            const string Option2 = nameof(Option2);

            const string ObsoleteOption2 = nameof(ObsoleteOption2);

            uint templateCalculationId = NewRandomUint();
            string calculationId = NewRandomString();
            
            string expectedEnumSourceCode =
                @$"Enum {EnumName}Options
    <Description(Description:=""{Option1}"")>
    {Option1}
    <Description(Description:=""{Option2}"")>
    {Option2}
    <ObsoleteItem()>
    <Description(Description:=""{ObsoleteOption2}"")>
    {ObsoleteOption2}
End Enum";

            GivenTheCalculations(NewCalculationWithNamespaceAndEnumDataType(EnumName,
                new[]
                {
                    Option1,
                    Option2
                }, calculationId: calculationId));
            AndTheObsoleteItems(NewObsoleteItem(_ => _.WithItemType(ObsoleteItemType.Calculation)),
                NewObsoleteItem(_ => _.WithItemType(ObsoleteItemType.EnumValue)
                    .WithEnumValueName(ObsoleteOption2)
                    .WithTemplateCalculationId(templateCalculationId)));
            AndTheFundings(NewFunding(NewRandomString(), _ => _.WithMappings((templateCalculationId, calculationId))));

            WhenTheCalculationNamespacesAreBuilt();

            ThenTheEnumDefinition(_ => StatementHasSourceCodeContaining(_, expectedEnumSourceCode));
        }

        [TestMethod]
        public void EachNamespaceClassContainsAMethodToInitialiseAllOfTheSharedContextAndNamespaces()
        {
            const string one = "One";

            string[] expectedInitialiseMethod =
            {
                @"Public Initialise As Action(Of CalculationContext) = Sub(calculationContext)
Datasets = calculationContext.Datasets
Provider = calculationContext.Provider

One = calculationContext.One
Calculations = calculationContext.Calculations"
            };

            GivenTheCalculations(NewCalculationWithNamespace(one));

            WhenTheCalculationNamespacesAreBuilt();

            ThenResultContainsNamespaceDefinitionsFor(one, Additional);
            AndTheNamespaceDefinition(Additional, ns => HasSourceCodeContaining(ns, expectedInitialiseMethod));
            AndTheNamespaceDefinition(one, ns => HasSourceCodeContaining(ns, expectedInitialiseMethod));
        }

        [TestMethod]
        public void EachNamespaceClassContainsPublicFieldsToAccessItsCalculationsAsFunctionPointers()
        {
            const string one = "One";

            string[] expectedFunctionPointers =
            {
                "Public One As Func(Of Decimal?) = Nothing",
                "Public Two As Func(Of Decimal?) = Nothing"
            };

            GivenTheCalculations(NewCalculationWithNamespace(one, "One"), NewCalculationWithNamespace(one, "Two"));

            WhenTheCalculationNamespacesAreBuilt();

            ThenResultContainsNamespaceDefinitionsFor(one, Additional);
            AndTheNamespaceDefinition(Additional, ns => HasSourceCodeNotContaining(ns, expectedFunctionPointers));
            AndTheNamespaceDefinition(one, ns => HasSourceCodeContaining(ns, expectedFunctionPointers));
        }

        [TestMethod]
        public void EachNamespaceClassContainsPropertiesToAccessAllOtherNamespacesAndTheCalcContext()
        {
            const string one = "One";

            string[] expectedPropertyDefinitions =
            {
                "PublicPropertyOneAsOneCalculations",
                "PublicPropertyCalculationsAsAdditionalCalculations"
            };

            GivenTheCalculations(NewCalculationWithNamespace(one));

            WhenTheCalculationNamespacesAreBuilt();

            ThenResultContainsNamespaceDefinitionsFor(one, Additional);
            AndTheNamespaceDefinition(Additional, ns => HasSourceCodeContaining(ns, expectedPropertyDefinitions));
            AndTheNamespaceDefinition(one, ns => HasSourceCodeContaining(ns, expectedPropertyDefinitions));
        }

        [TestMethod]
        public void AlwaysCreatesAdditionalCalculationsEvenIfEmpty()
        {
            WhenTheCalculationNamespacesAreBuilt();

            ThenResultContainsNamespaceDefinitionsFor(Additional);
        }

        private void ThenTheEnumDefinition(Func<StatementSyntax, bool> predicate)
        {
            _result.EnumsDefinitions.Any(predicate).Should().BeTrue();
        }

        private void GivenTheCalculations(params Calculation[] calculations)
        {
            _calculations.AddRange(calculations);
        }

        private void AndTheObsoleteItems(params ObsoleteItem[] obsoleteItems)
            => _obsoleteItems.AddRange(obsoleteItems);

        private void AndTheFundings(params (string key, Funding funding)[] fundings)
        {
            foreach ((string key, Funding funding) funding in fundings)
            {
                _funding[funding.key] = funding.funding;
            }
        }

        private void WhenTheCalculationNamespacesAreBuilt()
        {
            _result = _builder.BuildNamespacesForCalculations(_calculations, _obsoleteItems, _funding);
        }

        private void ThenResultContainsNamespaceDefinitionsFor(params string[] expectedNamespaces)
        {
            _result.InnerClasses
                .Select(_ => _.Namespace)
                .Should()
                .BeEquivalentTo(expectedNamespaces);
        }

        private void AndTheNamespaceDefinition(string @namespace,
            Func<NamespaceClassDefinition, bool> predicate)
        {
            NamespaceClassDefinition namespaceDefinition = _result.InnerClasses.FirstOrDefault(_ => _.Namespace == @namespace);

            namespaceDefinition.Should()
                .NotBeNull();

            predicate(namespaceDefinition)
                .Should()
                .BeTrue();
        }

        private Calculation NewCalculationWithNamespace(string @namespace,
            string sourceCodeName = null)
        {
            return NewCalculation(_ => _.WithCalculationNamespaceType(CalculationNamespace.Template)
                .WithFundingStream(NewReference(rf => rf.WithId(@namespace)))
                .WithSourceCodeName(sourceCodeName));
        }

        private Calculation NewCalculationWithNamespaceAndEnumDataType(string name,
            IEnumerable<string> allowedEnumTypeValues,
            string sourceCodeName = null,
            string calculationId = null)
        {
            return NewCalculation(_ => _.WithCalculationNamespaceType(CalculationNamespace.Template)
                .WithId(calculationId)
                .WithSourceCodeName(sourceCodeName)
                .WithName(name)
                .WithDataType(CalculationDataType.Enum)
                .WithAllowedEnumTypeValues(allowedEnumTypeValues));
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

        private bool HasSourceCodeContaining(NamespaceClassDefinition namespaceClassDefinition,
            params string[] expectedSourceCode)
        {
            string classSourceCode = GetSourceForClassBlock(namespaceClassDefinition.ClassBlockSyntax);

            return expectedSourceCode.All(expectedSourceCodeSnippet
                => classSourceCode.Contains(expectedSourceCodeSnippet));
        }

        private bool StatementHasSourceCodeContaining(StatementSyntax statementSyntax,
            params string[] expectedSourceCode)
        {
            string classSourceCode = GetSourceForClassBlock(statementSyntax);

            return expectedSourceCode.All(expectedSourceCodeSnippet
                => classSourceCode.Contains(expectedSourceCodeSnippet));
        }

        private bool HasSourceCodeNotContaining(NamespaceClassDefinition namespaceClassDefinition,
            params string[] expectedSourceCode) =>
            !HasSourceCodeContaining(namespaceClassDefinition, expectedSourceCode);

        private string GetSourceForClassBlock(StatementSyntax classBlockSyntax)
        {
            CompilationUnitSyntax syntaxTree = SyntaxFactory
                .CompilationUnit()
                .WithOptions(new SyntaxList<OptionStatementSyntax>(new[]
                {
                    SyntaxFactory.OptionStatement(SyntaxFactory.Token(SyntaxKind.StrictKeyword),
                        SyntaxFactory.Token(SyntaxKind.OnKeyword))
                }))
                .WithImports(StandardImports())
                .WithMembers(SyntaxFactory.SingletonList(classBlockSyntax));

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

        private ObsoleteItem NewObsoleteItem(Action<ObsoleteItemBuilder> setUp = null)
        {
            ObsoleteItemBuilder obsoleteItemBuilder = new ObsoleteItemBuilder();

            setUp?.Invoke(obsoleteItemBuilder);
            
            return obsoleteItemBuilder.Build();
        }

        private (string, Funding) NewFunding(string fundingStream,
            Action<FundingBuilder> setUp = null)
        {
            FundingBuilder fundingBuilder = new FundingBuilder();

            setUp?.Invoke(fundingBuilder);
            
            return (fundingStream, fundingBuilder.Build());
        }

        private uint NewRandomUint() => (uint) new RandomNumberBetween(1, int.MaxValue);

        private string NewRandomString() => new RandomString();
    }
}