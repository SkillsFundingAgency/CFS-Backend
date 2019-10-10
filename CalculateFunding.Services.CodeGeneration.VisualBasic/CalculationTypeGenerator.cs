using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Calculation = CalculateFunding.Models.Calcs.Calculation;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class CalculationTypeGenerator : VisualBasicTypeGenerator
    {
        private readonly CompilerOptions _compilerOptions;

        public CalculationTypeGenerator(CompilerOptions compilerOptions)
        {
            Guard.ArgumentNotNull(compilerOptions, nameof(compilerOptions));

            _compilerOptions = compilerOptions;
        }

        public IEnumerable<SourceFile> GenerateCalcs(IEnumerable<Calculation> calculations)
        {
            SyntaxList<OptionStatementSyntax> optionsList = new SyntaxList<OptionStatementSyntax>(new[]
            {
                SyntaxFactory.OptionStatement(SyntaxFactory.Token(SyntaxKind.StrictKeyword),
                    SyntaxFactory.Token(_compilerOptions.OptionStrictEnabled ? SyntaxKind.OnKeyword : SyntaxKind.OffKeyword))
            });

            SyntaxList<ImportsStatementSyntax> standardImports = StandardImports();

            string identifier = GenerateIdentifier("CalculationContext");

            SyntaxTokenList modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            ClassStatementSyntax classStatement = SyntaxFactory
                .ClassStatement(identifier)
                .WithModifiers(modifiers);

            SyntaxList<InheritsStatementSyntax> inherits = SyntaxFactory.SingletonList(SyntaxFactory.InheritsStatement(_compilerOptions.UseLegacyCode
                ? SyntaxFactory.ParseTypeName("LegacyBaseCalculation")
                : SyntaxFactory.ParseTypeName("BaseCalculation")));

            IEnumerable<StatementSyntax> methods = CreateMembers(calculations);
            
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

        private IEnumerable<StatementSyntax> CreateMembers(IEnumerable<Calculation> calculations)
        {
            yield return ParseSourceCodeToStatementSyntax("Public StackFrameStartingCount As Integer = 0");
            yield return ParseSourceCodeToStatementSyntax("Public Property Dictionary As Dictionary(Of String, String()) = New Dictionary(Of String, String())(2)");
            yield return ParseSourceCodeToStatementSyntax("Public Property DictionaryValues As Dictionary(Of String, Decimal?) = New Dictionary(Of String, Decimal?)(2)");

            if (calculations == null) yield break;

            NamespaceBuilderResult namespaceBuilderResult = new CalculationNamespaceBuilder(_compilerOptions)
                .BuildNamespacesForCalculations(calculations);

            foreach (StatementSyntax propertiesDefinition in namespaceBuilderResult.PropertiesDefinitions)
            {
                yield return propertiesDefinition;
            }

            IEnumerable<NamespaceClassDefinition> namespaceClassDefinitions = 
                namespaceBuilderResult.InnerClasses;

            foreach (NamespaceClassDefinition namespaceClassDefinition in namespaceClassDefinitions)
            {
                yield return namespaceClassDefinition.ClassBlockSyntax;
            }

            yield return CreateMainMethod(calculations,namespaceClassDefinitions);
        }

        private StatementSyntax CreateMainMethod(IEnumerable<Calculation> calcs, 
            IEnumerable<NamespaceClassDefinition> namespaceDefinitions)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine();
            builder.AppendLine("<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>");
            builder.AppendLine("Public Function MainCalc As Dictionary(Of String, String())");
            builder.AppendLine();

            if (_compilerOptions.UseDiagnosticsMode)
            {
                builder.AppendLine("Dim sw As New System.Diagnostics.Stopwatch()");
            }

            foreach (NamespaceClassDefinition namespaceDefinition in namespaceDefinitions)
            {
                builder.AppendLine($"{namespaceDefinition.Namespace} = New {namespaceDefinition.ClassName}()");
            }

            foreach (NamespaceClassDefinition namespaceDefinition in namespaceDefinitions)
            {
                builder.AppendLine($"{namespaceDefinition.Namespace}.Initialise(Me)");
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

                builder.AppendLine($"{calc.Namespace}.{calc.Current.SourceCodeName}()");

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
                    builder.AppendLine($"Dictionary.Add(\"{calc.Id}\", {{\"\", ex.GetType().Name, ex.Message, sw.Elapsed.ToString(), ex.StackTrace }})");
                    builder.AppendLine("End Try");
                }

                builder.AppendLine();
            }

            builder.AppendLine("return Dictionary");
            builder.AppendLine("End Function");
            builder.AppendLine();

            return ParseSourceCodeToStatementSyntax(builder);
        }
    }
}
