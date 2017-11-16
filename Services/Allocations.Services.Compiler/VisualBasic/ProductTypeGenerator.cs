using System.Collections.Generic;
using System.Linq;
using System.Text;
using Allocations.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Allocations.Services.Compiler.VisualBasic
{
    public class ProductTypeGenerator : VisualBasicTypeGenerator
    {
        public CompilationUnitSyntax GenerateCalcs(Budget budget)
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

        private static IEnumerable<StatementSyntax> Methods(Budget budget)
        {
            foreach (var budgetDatasetDefinition in budget.DatasetDefinitions)
            {
                yield return GetDatasetProperties(budgetDatasetDefinition);
            }
            foreach (var fundingPolicy in budget.FundingPolicies)
            {
                foreach (var allocationLine in fundingPolicy.AllocationLines ?? new List<AllocationLine>())
                {
                    foreach (var partialClass in GetMethods(allocationLine))
                    {
                        yield return partialClass;
                    }

                }
            }
        }

        private static IEnumerable<StatementSyntax> GetMethodStatements(Product product)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(/*product.Calculation.SourceCode ?? */$"Throw new NotImplementedException(\"{product.Name} is not implemented\")");


            return tree?.GetRoot()?.DescendantNodes()?.OfType<StatementSyntax>();     
        }

        private static StatementSyntax GetMethod(Product product)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Public Function {Identifier(product.Name)} As Decimal");
            builder.Append(product.Calculation.SourceCode ?? "Throw new NotImplementedException(\"{product.Name} is not implemented\")");
            builder.AppendLine();
            builder.AppendLine("End Function");
            builder.AppendLine();
            var tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());

            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                .FirstOrDefault();
        }


        private static IEnumerable<StatementSyntax> GetMethods(AllocationLine allocationLine)
        {
            foreach (var productFolder in allocationLine.ProductFolders ?? new List<ProductFolder>())
            {
                foreach (var product in productFolder.Products ?? new List<Product>())
                {
                    var method = GetMethod(product);
                    yield return method;
                }
            }
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
