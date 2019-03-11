using CalculateFunding.Models.Calcs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Calculation = CalculateFunding.Models.Calcs.Calculation;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class CalculationTypeGenerator : VisualBasicTypeGenerator
    {
        public IEnumerable<SourceFile> GenerateCalcs(BuildProject buildProject, IEnumerable<Calculation> calculations)
        {
            var syntaxTree = SyntaxFactory.CompilationUnit()
                .WithImports(StandardImports())
                
                .WithMembers(SyntaxFactory.SingletonList<StatementSyntax>(
            SyntaxFactory.ClassBlock(
                SyntaxFactory.ClassStatement(
                        GenerateIdentifier("Calculations")
                    )
                    
                    .WithModifiers(
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword))),
                SyntaxFactory.SingletonList(SyntaxFactory.InheritsStatement(SyntaxFactory.ParseTypeName("BaseCalculation"))),
                new SyntaxList<ImplementsStatementSyntax>(),
                SyntaxFactory.List(Methods(buildProject, calculations)),
                SyntaxFactory.EndClassStatement()
            )
  
                    ))
                .NormalizeWhitespace();

            yield return new SourceFile {FileName = "Calculations.vb", SourceCode = syntaxTree.ToFullString()};
        }

        private static IEnumerable<StatementSyntax> Methods(BuildProject buildProject, IEnumerable<Calculation> calculations)
        {

            yield return GetDatasetProperties();
            yield return GetProviderProperties();

            if (calculations != null)
            {
                foreach (var calc in calculations)
                {
                    yield return GetFunc(calc);
                }

                yield return GetMainMethod(calculations);
            }
        }

        private static StatementSyntax GetFunc(Calculation calc)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"<Calculation(Id := \"{calc.Id}\", Name := \"{calc.Name}\")>");
            if (calc.CalculationSpecification != null)
            {
                builder.AppendLine($"<CalculationSpecification(Id := \"{calc.CalculationSpecification.Id}\", Name := \"{calc.CalculationSpecification.Name}\")>");
            }

            if (calc.AllocationLine != null)
            {
                builder.AppendLine($"<AllocationLine(Id := \"{calc.AllocationLine.Id}\", Name := \"{calc.AllocationLine.Name}\")>");
            }

            if (calc.Policies != null)
            {
                foreach (var policySpecification in calc.Policies)
                {
                    builder.AppendLine($"<PolicySpecification(Id := \"{policySpecification.Id}\", Name := \"{policySpecification.Name}\")>");
                }
            }

            if (!string.IsNullOrWhiteSpace(calc.Description))
            {
                builder.AppendLine($"<Description(Description := \"{calc.Description?.Replace("\"", "\"\"")}\")>");
            }

            if (!string.IsNullOrWhiteSpace(calc.Current?.SourceCode))
            {
                calc.Current.SourceCode = QuoteAggregateFunctionCalls(calc.Current.SourceCode);
            }

            builder.AppendLine($"Dim {GenerateIdentifier(calc.Name)} As Func(Of decimal?) = nothing");
            
            builder.AppendLine();

            builder.AppendLine($"Dim {GenerateIdentifier(calc.Name)} As Func(Of decimal?) = nothing");
            
            builder.AppendLine();

            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());


            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                .FirstOrDefault();
        }

        private static StatementSyntax GetMainMethod(IEnumerable<Calculation> calcs)
        {
            var builder = new StringBuilder();

            builder.AppendLine();
            builder.AppendLine($"Public Function MainCalc As Dictionary(Of String, String())");
            builder.AppendLine();
            builder.AppendLine("Dim frameCount = New System.Diagnostics.StackTrace().FrameCount");
            builder.AppendLine("If frameCount > 1000 Then");
            builder.AppendLine("Throw New Exception(\"The system detected a stackoverflow, this is probably due to recursive methods stuck in an infinite loop\")");
            builder.AppendLine("End If");
            builder.AppendLine("Dim dictionary as new Dictionary(Of String, String())");

            foreach (var calc in calcs)
            {
                builder.AppendLine($"{GenerateIdentifier(calc.Name)} = Function() As decimal?");
                builder.AppendLine();
                builder.AppendLine($"#ExternalSource(\"{calc.Id}|{calc.Name}\", 1)");
                builder.AppendLine();
                builder.Append(calc.Current?.SourceCode ?? CodeGenerationConstants.VisualBasicDefaultSourceCode);
                builder.AppendLine();
                builder.AppendLine("#End ExternalSource");
                builder.AppendLine();
                builder.AppendLine("End Function");
               
            }

            builder.AppendLine();

            foreach (var calc in calcs)
            {
                builder.AppendLine("Try");
                builder.AppendLine($"Dim calcResult As Nullable(Of Decimal) = {GenerateIdentifier(calc.Name)}()");
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


        private static StatementSyntax GetDatasetProperties()
        {
            return SyntaxFactory.PropertyStatement(GenerateIdentifier("Datasets"))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
	            .WithAsClause(
		            SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName(GenerateIdentifier("Datasets"))));
		}

        private static StatementSyntax GetProviderProperties()
        {
            return SyntaxFactory.PropertyStatement(GenerateIdentifier("Provider"))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAsClause(
                    SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName(GenerateIdentifier("Provider"))));
        }

        public static string QuoteAggregateFunctionCalls(string sourceCode)
        {
            sourceCode = Regex.Replace(sourceCode, @"\s+(?=[^(\]]*\))", "");

            Regex x = new Regex(@"(\bMin\b|\bAvg\b|\bMax\b|\bSum\b)()(.*?\)$)");

            foreach (Match match in x.Matches(sourceCode))
            {
                string result = match.Value
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
