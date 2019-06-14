using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private CompilerOptions _compilerOptions;

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

            string identifier = GenerateIdentifier("Calculations");

            SyntaxTokenList modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            ClassStatementSyntax classStatement = SyntaxFactory
                .ClassStatement(identifier)
                .WithModifiers(modifiers);

            SyntaxList<InheritsStatementSyntax> inherits = SyntaxFactory.SingletonList(SyntaxFactory.InheritsStatement(_compilerOptions.UseLegacyCode
                ? SyntaxFactory.ParseTypeName("LegacyBaseCalculation")
                : SyntaxFactory.ParseTypeName("BaseCalculation")));

            IEnumerable<StatementSyntax> methods = CreateMethods(calculations);

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

        private IEnumerable<StatementSyntax> CreateMethods(IEnumerable<Calculation> calculations)
        {
            yield return CreateDatasetProperties();
            yield return CreateProviderProperties();

            if (calculations != null)
            {
                foreach (Calculation calc in calculations)
                {
                    yield return CreateCalculationVariables(calc);
                }

                yield return CreateMainMethod(calculations);
            }
        }

        private StatementSyntax CreateCalculationVariables(Calculation calc)
        {
            if (string.IsNullOrWhiteSpace(calc.SourceCodeName)) throw new InvalidOperationException($"Calculation source code name is not populated for calc {calc.Id }");

            StringBuilder builder = new StringBuilder();

            // Add attributes to describe calculation and calculation specification
            builder.AppendLine($"<Calculation(Id := \"{calc.Id}\", Name := \"{calc.Name}\")>");
            if (calc.CalculationSpecification != null)
            {
                builder.AppendLine($"<CalculationSpecification(Id := \"{calc.CalculationSpecification.Id}\", Name := \"{calc.CalculationSpecification.Name}\")>");
            }

            if (calc.AllocationLine != null)
            {
                // Add attribute for allocation line
                builder.AppendLine($"<AllocationLine(Id := \"{calc.AllocationLine.Id}\", Name := \"{calc.AllocationLine.Name}\")>");
            }

            if (calc.Policies != null)
            {
                // Add attributes for policies
                foreach (Common.Models.Reference policySpecification in calc.Policies)
                {
                    builder.AppendLine($"<PolicySpecification(Id := \"{policySpecification.Id}\", Name := \"{policySpecification.Name}\")>");
                }
            }

            // Add attribute for calculation description
            if (!string.IsNullOrWhiteSpace(calc.Description))
            {
                builder.AppendLine($"<Description(Description := \"{calc.Description?.Replace("\"", "\"\"")}\")>");
            }

            if (!string.IsNullOrWhiteSpace(calc.Current?.SourceCode))
            {
                calc.Current.SourceCode = QuoteAggregateFunctionCalls(calc.Current.SourceCode);
            }

            builder.AppendLine($"Dim {calc.SourceCodeName} As Func(Of decimal?) = nothing");

            builder.AppendLine();

            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());

            return tree
                .GetRoot()
                .DescendantNodes()
                .OfType<StatementSyntax>()
                .FirstOrDefault();
        }

        private StatementSyntax CreateMainMethod(IEnumerable<Calculation> calcs)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine();
            builder.AppendLine($"Public Function MainCalc As Dictionary(Of String, String())");
            builder.AppendLine();
            builder.AppendLine("Dim stackFrameStartingCount as integer = 0");

            if (_compilerOptions.UseDiagnosticsMode)
            {
                builder.AppendLine("Dim sw As New System.Diagnostics.Stopwatch()");
            }

            builder.AppendLine($"Dim dictionary as new Dictionary(Of String, String())({calcs.Count()})");
            builder.AppendLine($"Dim dictionaryValues as new Dictionary(Of String, Decimal?)({calcs.Count()})");

            foreach (Calculation calc in calcs)
            {
                builder.AppendLine();

                builder.AppendLine($"{calc.SourceCodeName} = Function() As decimal?");

                builder.AppendLine($"Dim existingCacheItem as String() = Nothing");
                builder.AppendLine($"If dictionary.TryGetValue(\"{calc.Id}\", existingCacheItem) Then");
                builder.AppendLine($"Dim existingCalculationResultDecimal As Decimal? = Nothing");
                builder.AppendLine($"   If dictionaryValues.TryGetValue(\"{calc.Id}\", existingCalculationResultDecimal) Then");
                builder.AppendLine($"        Return existingCalculationResultDecimal");
                builder.AppendLine("    End If");

                builder.AppendLine("    If existingCacheItem.Length > 2 Then");
                builder.AppendLine($"       Dim exceptionType as String = existingCacheItem(1)");
                builder.AppendLine("        If Not String.IsNullOrEmpty(exceptionType) then");
                builder.AppendLine("            Dim exceptionMessage as String = existingCacheItem(2)");
                builder.AppendLine($"           Throw New ReferencedCalculationFailedException(\"{calc.Name} failed due to exception type:\" + exceptionType  + \" with message: \" + exceptionMessage)");
                builder.AppendLine("        End If");
                builder.AppendLine("    End If");
                builder.AppendLine("End If");


                builder.AppendLine("Dim userCalculationCodeImplementation As Func(Of Decimal?) = Function() as Decimal?");
                //builder.AppendLine("Throw New System.Exception(New System.Diagnostics.StackTrace().FrameCount.ToString() + \" started with\")");

                builder.AppendLine("Dim frameCount = New System.Diagnostics.StackTrace().FrameCount");
                builder.AppendLine("If frameCount > stackFrameStartingCount + 40 Then");
                builder.AppendLine($"   Throw New CalculationStackOverflowException(\"The system detected a stackoverflow from {calc.Name}, this is probably due to recursive methods stuck in an infinite loop\")");
                builder.AppendLine("End If");

                builder.AppendLine($"#ExternalSource(\"{calc.Id}|{calc.Name}\", 1)");
                builder.AppendLine();
                builder.Append(calc.Current?.SourceCode ?? CodeGenerationConstants.VisualBasicDefaultSourceCode);
                builder.AppendLine();
                builder.AppendLine("#End ExternalSource");

                builder.AppendLine("End Function");
                builder.AppendLine();

                builder.AppendLine("Try");

                builder.AppendLine("Dim executedUserCodeCalculationResult As Nullable(Of Decimal) = userCalculationCodeImplementation()");
                builder.AppendLine();
                builder.AppendLine($"dictionary.Add(\"{calc.Id}\", {{If(executedUserCodeCalculationResult.HasValue, executedUserCodeCalculationResult.ToString(), \"\")}})");
                builder.AppendLine($"dictionaryValues.Add(\"{calc.Id}\", executedUserCodeCalculationResult)");
                builder.AppendLine("Return executedUserCodeCalculationResult");
                builder.AppendLine("Catch ex as System.Exception");
                builder.AppendLine($"   dictionary.Add(\"{calc.Id}\", {{\"\", ex.GetType().Name, ex.Message}})");
                builder.AppendLine("    Throw");
                builder.AppendLine("End Try");
                builder.AppendLine();

                builder.AppendLine("End Function");
            }

            builder.AppendLine();

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
                builder.AppendLine("stackFrameStartingCount = New System.Diagnostics.StackTrace().FrameCount");

                builder.AppendLine($"{calc.SourceCodeName}()");

                builder.AppendLine();

                builder.AppendLine("Catch ex as Exception");
                builder.AppendLine("' Already added to dictionary in normal func, we should stop this from bubbling reference exception for main calc call");
                builder.AppendLine("End Try");

                if (_compilerOptions.UseDiagnosticsMode)
                {
                    builder.AppendLine("    sw.stop()");
                    builder.AppendLine($"dictionary.Add(\"{calc.Id}\", {{If(calcResult.HasValue, calcResult.ToString(), \"\"),\"\", \"\", sw.Elapsed.ToString()}})");
                    builder.AppendLine("Catch ex as System.Exception");
                    builder.AppendLine("    sw.stop()");
                    builder.AppendLine($"dictionary.Add(\"{calc.Id}\", {{\"\", ex.GetType().Name, ex.Message, sw.Elapsed.ToString() }})");
                    builder.AppendLine("End Try");
                }
                else
                {

                }

                builder.AppendLine();
            }

            builder.AppendLine("return dictionary");
            builder.AppendLine("End Function");
            builder.AppendLine();
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());

            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
               .FirstOrDefault();
        }

        private static StatementSyntax CreateDatasetProperties()
        {
            return SyntaxFactory.PropertyStatement(GenerateIdentifier("Datasets"))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAsClause(
                    SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName(GenerateIdentifier("Datasets"))));
        }

        private static StatementSyntax CreateProviderProperties()
        {
            return SyntaxFactory.PropertyStatement(GenerateIdentifier("Provider"))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAsClause(
                    SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName(GenerateIdentifier("Provider"))));
        }

        public static string QuoteAggregateFunctionCalls(string sourceCode)
        {
            Regex x = new Regex(@"(\b(?<!Math.)Min\b|\b(?<!Math.)Avg\b|\b(?<!Math.)Max\b|\b(?<!Math.)Sum\b)()(.*?\))");

            foreach (Match match in x.Matches(sourceCode))
            {
                string strippedText = Regex.Replace(match.Value, @"\s+", string.Empty);

                string result = strippedText
                    .Replace("Sum(", "Sum(\"")
                    .Replace("Max(", "Max(\"")
                    .Replace("Min(", "Min(\"")
                    .Replace("Avg(", "Avg(\"")
                    .Replace(")", "\")");

                if (match.Success)
                {
                    sourceCode = sourceCode.Replace(match.Value, result);
                }
            }

            return sourceCode;
        }
    }
}
