using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using FundingLine = CalculateFunding.Models.Calcs.FundingLine;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class CalculationTypeGenerator : VisualBasicTypeGenerator
    {
        private readonly CompilerOptions _compilerOptions;
        private readonly IFundingLineRoundingSettings _fundingLineRoundingSettings;
        private readonly ITypeIdentifierGenerator _typeIdentifierGenerator;

        public CalculationTypeGenerator(CompilerOptions compilerOptions,
            IFundingLineRoundingSettings fundingLineRoundingSettings)
        {
            Guard.ArgumentNotNull(compilerOptions, nameof(compilerOptions));
            Guard.ArgumentNotNull(fundingLineRoundingSettings, nameof(fundingLineRoundingSettings));

            _compilerOptions = compilerOptions;
            _fundingLineRoundingSettings = fundingLineRoundingSettings;

            _typeIdentifierGenerator = new VisualBasicTypeIdentifierGenerator();
        }

        public IEnumerable<SourceFile> GenerateCalcs(IEnumerable<Calculation> calculations, IDictionary<string, Funding> funding)
        {
            SyntaxList<OptionStatementSyntax> optionsList = new SyntaxList<OptionStatementSyntax>(new[]
            {
                SyntaxFactory.OptionStatement(SyntaxFactory.Token(SyntaxKind.StrictKeyword),
                    SyntaxFactory.Token(_compilerOptions.OptionStrictEnabled ? SyntaxKind.OnKeyword : SyntaxKind.OffKeyword))
            });

            SyntaxList<ImportsStatementSyntax> standardImports = StandardImports();

            string identifier = _typeIdentifierGenerator.GenerateIdentifier("CalculationContext");

            SyntaxTokenList modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            ClassStatementSyntax classStatement = SyntaxFactory
                .ClassStatement(identifier)
                .WithModifiers(modifiers);

            SyntaxList<InheritsStatementSyntax> inherits = SyntaxFactory.SingletonList(SyntaxFactory.InheritsStatement(_compilerOptions.UseLegacyCode
                ? SyntaxFactory.ParseTypeName("LegacyBaseCalculation")
                : SyntaxFactory.ParseTypeName("BaseCalculation")));

            IEnumerable<StatementSyntax> methods = CreateMembers(calculations, funding);
            
            ClassBlockSyntax classBlock = SyntaxFactory.ClassBlock(classStatement,
                inherits,
                new SyntaxList<ImplementsStatementSyntax>(),
                SyntaxFactory.List(methods),
                SyntaxFactory.EndClassStatement());

            SyntaxList<StatementSyntax> members = SyntaxFactory.SingletonList<StatementSyntax>(classBlock);

            CompilationUnitSyntax syntaxTree = SyntaxFactory.CompilationUnit().WithOptions(optionsList);
            syntaxTree = syntaxTree.WithImports(standardImports);
            syntaxTree = syntaxTree.WithMembers(members);
            
            try
            {
                syntaxTree = syntaxTree.NormalizeWhitespace();
            }
            catch (Exception e)
            {
                throw new Exception($"Error compiling source code. Please check your code's structure is valid.  {e.Message}", e);
            }

            string sourceCode = syntaxTree.ToFullString();

            yield return new SourceFile { FileName = "Calculations.vb", SourceCode = sourceCode };
        }

        private IEnumerable<StatementSyntax> CreateMembers(IEnumerable<Calculation> calculations, IDictionary<string, Funding> funding)
        {
            yield return ParseSourceCodeToStatementSyntax("Public StackFrameStartingCount As Integer = 0");
            yield return ParseSourceCodeToStatementSyntax("Public Property Dictionary As Dictionary(Of String, String()) = New Dictionary(Of String, String())(2)");
            yield return ParseSourceCodeToStatementSyntax("Public Property DictionaryDecimalValues As Dictionary(Of String, Decimal?) = New Dictionary(Of String, Decimal?)(2)");
            yield return ParseSourceCodeToStatementSyntax("Public Property DictionaryBooleanValues As Dictionary(Of String, Boolean?) = New Dictionary(Of String, Boolean?)(2)");
            yield return ParseSourceCodeToStatementSyntax("Public Property DictionaryStringValues As Dictionary(Of String, String) = New Dictionary(Of String, String)(2)");
            yield return ParseSourceCodeToStatementSyntax("Public Property FundingLineDictionary As Dictionary(Of String, String()) = New Dictionary(Of String, String())(2)");
            yield return ParseSourceCodeToStatementSyntax("Public Property FundingLineDictionaryValues As Dictionary(Of String, Decimal?) = New Dictionary(Of String, Decimal?)(2)");

            if (calculations == null) yield break;

            HashSet<string> calculationIds = calculations.Select(_ => _.Current.CalculationId).ToHashSet();

            // filter out calculations which are not in scope
            foreach (string fundingStreamId in funding.Keys)
            {
                funding[fundingStreamId].FundingLines = funding[fundingStreamId].FundingLines.Select(fl =>
                {
                    fl.Calculations = fl.Calculations?.Where(calc => funding[fundingStreamId].Mappings.ContainsKey(calc.Id) && calculationIds.Contains(funding[fundingStreamId].Mappings[calc.Id]));
                    return fl;
                });
            }

            IEnumerable<NamespaceBuilderResult> namespaceBuilderResults = new[] {
                new CalculationNamespaceBuilder(_compilerOptions).BuildNamespacesForCalculations(calculations),
                new FundingLineNamespaceBuilder().BuildNamespacesForFundingLines(funding, _fundingLineRoundingSettings.DecimalPlaces)
            };

            foreach (NamespaceBuilderResult namespaceBuilderResult in namespaceBuilderResults)
            {
                foreach (StatementSyntax propertiesDefinition in namespaceBuilderResult.PropertiesDefinitions)
                {
                    yield return propertiesDefinition;
                }

                foreach (StatementSyntax enumsDefinition in namespaceBuilderResult.EnumsDefinitions)
                {
                    yield return enumsDefinition;
                }

                foreach (NamespaceClassDefinition namespaceClassDefinition in namespaceBuilderResult.InnerClasses)
                {
                    yield return namespaceClassDefinition.ClassBlockSyntax;
                }
            }

            yield return CreateMainMethod(calculations, funding, namespaceBuilderResults.SelectMany(_ => _.InnerClasses));
        }

        private StatementSyntax CreateMainMethod(IEnumerable<Calculation> calcs,
            IDictionary<string, Funding> funding,
            IEnumerable<NamespaceClassDefinition> namespaceDefinitions)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine();
            builder.AppendLine("<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>");
            builder.AppendLine(
                "Public MainCalc As Func(Of Boolean, (CalculationResults As Dictionary(Of String, String()), FundingLineResults As Dictionary(Of String, String()))) = Function(allCalculations)");
            builder.AppendLine();

            if (_compilerOptions.UseDiagnosticsMode)
            {
                builder.AppendLine("Dim sw As New System.Diagnostics.Stopwatch()");
            }

            foreach (NamespaceClassDefinition namespaceDefinition in namespaceDefinitions)
            {
                builder.AppendLine($"{namespaceDefinition.Variable} = New {namespaceDefinition.ClassName}()");
            }

            foreach (NamespaceClassDefinition namespaceDefinition in namespaceDefinitions)
            {
                builder.AppendLine($"{namespaceDefinition.Variable}.Initialise(Me)");
            }

            foreach (string @namespace in funding.Keys)
            {
                foreach (FundingLine fundingline in funding[@namespace].FundingLines)
                {
                    builder.AppendLine("Try");
                    builder.AppendLine();

                    // Reset baseline stack frame count before executing calc
                    builder.AppendLine("StackFrameStartingCount = New System.Diagnostics.StackTrace().FrameCount");

                    builder.AppendLine($"{_typeIdentifierGenerator.GenerateIdentifier(@namespace)}.FundingLines.{fundingline.SourceCodeName}()");

                    builder.AppendLine();

                    builder.AppendLine("Catch ex as Exception");
                    builder.AppendLine("' Already added to dictionary in normal func, we should stop this from bubbling reference exception for main calc call");
                    builder.AppendLine("End Try");

                    builder.AppendLine();
                }
            }

            foreach (Calculation calc in calcs)
            {
                if (_compilerOptions.UseDiagnosticsMode)
                {
                    builder.AppendLine("sw.reset()");
                    builder.AppendLine("sw.start()");
                }

                builder.AppendLine();

                builder.AppendLine("Try");
                builder.AppendLine();

                // Reset baseline stack frame count before executing calc
                builder.AppendLine("StackFrameStartingCount = New System.Diagnostics.StackTrace().FrameCount");

                builder.AppendLine($"{_typeIdentifierGenerator.GenerateIdentifier(calc.Namespace)}.{calc.Current.SourceCodeName}()");

                builder.AppendLine();

                builder.AppendLine("Catch ex as Exception");
                builder.AppendLine("' Already added to dictionary in normal func, we should stop this from bubbling reference exception for main calc call");
                builder.AppendLine("End Try");

                if (_compilerOptions.UseDiagnosticsMode)
                {
                    builder.AppendLine("    sw.stop()");
                    builder.AppendLine($"Dictionary.Add(\"{calc.Id}\", {{If(calcResult.HasValue, calcResult.ToString(), \"\"),\"\", \"\", sw.Elapsed.ToString()}})");
                    builder.AppendLine("Catch ex as System.Exception");
                    builder.AppendLine("    sw.stop()");
                    builder.AppendLine($"Dictionary.Add(\"{calc.Id}\", {{\"\", ex.GetType().Name, ex.Message, ex.StackTrace.Replace(Environment.NewLine, \" \"), sw.Elapsed.ToString() }})");
                    builder.AppendLine("End Try");
                }

                builder.AppendLine();
            }

            builder.AppendLine("Return (CalculationResults:=Dictionary, FundingLineResults:=FundingLineDictionary)");
            builder.AppendLine("End Function");
            builder.AppendLine();

            return ParseSourceCodeToStatementSyntax(builder);
        }
    }
}
