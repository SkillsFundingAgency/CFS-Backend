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
        private readonly bool _useSourceCodeNameForCalculations;

        public CalculationTypeGenerator(CompilerOptions compilerOptions, bool useSourceCodeNameForCalculations)
        {
            Guard.ArgumentNotNull(compilerOptions, nameof(compilerOptions));

            _compilerOptions = compilerOptions;
            _useSourceCodeNameForCalculations = useSourceCodeNameForCalculations;
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

            if (_useSourceCodeNameForCalculations)
            {
                builder.AppendLine($"Dim {calc.SourceCodeName} As Func(Of decimal?) = nothing");

                builder.AppendLine();
            }
            else
            {
                builder.AppendLine($"Dim {GenerateIdentifier(calc.Name)} As Func(Of decimal?) = nothing");

                builder.AppendLine();
            }

            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());


            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                .FirstOrDefault();
        }

        private StatementSyntax CreateMainMethod(IEnumerable<Calculation> calcs)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine();
            builder.AppendLine($"Public Function MainCalc As Dictionary(Of String, String())");
            builder.AppendLine();

            builder.AppendLine("Dim dictionary as new Dictionary(Of String, String())");

            foreach (Calculation calc in calcs)
            {
                if (_useSourceCodeNameForCalculations)
                {
                    builder.AppendLine($"{calc.SourceCodeName} = Function() As decimal?");
                }
                else
                {
                    builder.AppendLine($"{GenerateIdentifier(calc.Name)} = Function() As decimal?");
                }
                builder.AppendLine($"If dictionary.ContainsKey(\"{calc.Id}\") Then");
                builder.AppendLine($"   dim resOut as Decimal");
                builder.AppendLine($"   dim item as string = dictionary.Item(\"{calc.Id}\")(0)");
                builder.AppendLine("    dim parsed as boolean = [Decimal].TryParse(item, resOut)");
                builder.AppendLine("    if parsed = False then");
                builder.AppendLine("        return Nothing");
                builder.AppendLine("    else");
                builder.AppendLine("        return resOut");
                builder.AppendLine("    end if");
                builder.AppendLine("end if");
                builder.AppendLine("Dim frameCount = New System.Diagnostics.StackTrace().FrameCount");
                builder.AppendLine("If frameCount > 1000 Then");
                builder.AppendLine("Throw New Exception(\"The system detected a stackoverflow, this is probably due to recursive methods stuck in an infinite loop\")");
                builder.AppendLine("End If");
                builder.AppendLine($"#ExternalSource(\"{calc.Id}|{calc.Name}\", 1)");
                builder.AppendLine();
                builder.Append(calc.Current?.SourceCode ?? CodeGenerationConstants.VisualBasicDefaultSourceCode);
                builder.AppendLine();
                builder.AppendLine("#End ExternalSource");
                builder.AppendLine();
                builder.AppendLine("End Function");
            }

            builder.AppendLine();

            foreach (Calculation calc in calcs)
            {
                builder.AppendLine("Try");

                if (_useSourceCodeNameForCalculations)
                {
                    builder.AppendLine($"Dim calcResult As Nullable(Of Decimal) = {calc.SourceCodeName}()");
                }
                else
                {
                    builder.AppendLine($"Dim calcResult As Nullable(Of Decimal) = {GenerateIdentifier(calc.Name)}()");
                }

                builder.AppendLine($"dictionary.Add(\"{calc.Id}\", {{If(calcResult.HasValue, calcResult.ToString(), \"\"),\"\", \"\"}})");
                builder.AppendLine("Catch ex as System.Exception");
                builder.AppendLine($"dictionary.Add(\"{calc.Id}\", {{\"\", ex.GetType().Name, ex.Message}})");
                builder.AppendLine("End Try");
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
                string strippedText = Regex.Replace(match.Value, @"\s+", "");

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
