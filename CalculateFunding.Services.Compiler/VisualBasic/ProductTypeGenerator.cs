using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CalculateFunding.Services.Compiler.VisualBasic
{
    public class ProductTypeGenerator : VisualBasicTypeGenerator
    {
        public CompilationUnitSyntax GenerateCalcs(Implementation budget)
        {
            return SyntaxFactory.CompilationUnit()
                .WithImports(StandardImports())
                .WithMembers(
                    SyntaxFactory.SingletonList<StatementSyntax>(
            SyntaxFactory.ClassBlock(
                SyntaxFactory.ClassStatement(
                        Identifier("ProductCalculations")
                    )
                    .WithModifiers(
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword))),
                new SyntaxList<InheritsStatementSyntax>(),
                new SyntaxList<ImplementsStatementSyntax>(),
                SyntaxFactory.List(Methods(budget)),
                SyntaxFactory.EndClassStatement()
            )
  
                    ))
                .NormalizeWhitespace();
        }

        private static IEnumerable<StatementSyntax> Methods(Implementation budget)
        {
            foreach (var budgetDatasetDefinition in budget.DatasetDefinitions)
            {
                yield return GetDatasetProperties(budgetDatasetDefinition);
            }
            foreach (var calc in budget.Calculations)
            {
                yield return GetMethod(calc);
            }
        }


        private static StatementSyntax GetMethod(Calculation calc)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"<Calculation(Id := \"{calc.Id}\")>");
            if (calc.CalculationSpecification != null)
            {
                builder.AppendLine($"<CalculationSpecification(Id := \"{calc.CalculationSpecification.Id}\", Name := \"{calc.CalculationSpecification.Name}\")>");
            }
            if (calc.AllocationLine != null)
            {
                builder.AppendLine($"<AllocationLine(Id := \"{calc.AllocationLine.Id}\", Name := \"{calc.AllocationLine.Name}\")>");
            }
            if (calc.PolicySpecifications != null)
            {
                foreach (var policySpecification in calc.PolicySpecifications)
                {
                    builder.AppendLine($"<PolicySpecification(Id := \"{policySpecification.Id}\", Name := \"{policySpecification.Name}\")>");
                }
            }

            builder.AppendLine($"Public Function {Identifier(calc.Name)} As Decimal");
            builder.Append(calc.SourceCode ?? $"Throw new NotImplementedException(\"{calc.Name} is not implemented\")");
            builder.AppendLine();
            builder.AppendLine("End Function");
            builder.AppendLine();
            var tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());

            
            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                .FirstOrDefault();
        }


 

        private static StatementSyntax GetDatasetProperties(DatasetDefinition datasetDefinition)
        {
            return SyntaxFactory.PropertyStatement(Identifier(datasetDefinition.Name))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAsClause(
                    SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName(Identifier($"{datasetDefinition.Name}Dataset"))));
        }

    }
}
