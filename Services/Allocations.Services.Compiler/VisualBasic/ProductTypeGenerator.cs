using System.Collections.Generic;
using System.Linq;
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
                    SyntaxFactory.List<StatementSyntax>(
                        Classes(budget)
                    ))
                .NormalizeWhitespace();
        }

        private static IEnumerable<StatementSyntax> Classes(Budget budget)
        {
            foreach (var fundingPolicy in budget.FundingPolicies)
            {
                foreach (var allocationLine in fundingPolicy.AllocationLines)
                {
                    yield return SyntaxFactory.ClassBlock(
                        SyntaxFactory.ClassStatement(
                                Identifier("ProductCalculations")
                            )
                            .WithModifiers(
                                SyntaxFactory.TokenList(
                                    SyntaxFactory.Token(SyntaxKind.PublicKeyword))),
                        new SyntaxList<InheritsStatementSyntax>(),
                        new SyntaxList<ImplementsStatementSyntax>(),
                        SyntaxFactory.List(budget.DatasetDefinitions.Select(GetMembers)),
                        SyntaxFactory.EndClassStatement()
                    );

                    foreach (var partialClass in GetProductPartials(allocationLine))
                    {
                        yield return partialClass;
                    }

                }
            }
        }

        private static StatementSyntax GetMethod(Product product)
        {
            if (product.Calculation?.SourceCode != null)
            {
                var tree = SyntaxFactory.ParseSyntaxTree(product.Calculation.SourceCode);

                var method = tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                    .FirstOrDefault();


                return method;
            }
            return null;
        }

        private static IEnumerable<StatementSyntax> GetProductPartials(AllocationLine allocationLine)
        {
            foreach (var productFolder in allocationLine.ProductFolders)
            {
                foreach (var product in productFolder.Products)
                {
                    var method = GetMethod(product);
                    if (method != null)
                    {
                        var partialClass = SyntaxFactory.ClassBlock(
                            SyntaxFactory.ClassStatement(
                                    Identifier("ProductCalculations")
                                )
                                .WithModifiers(
                                    SyntaxFactory.TokenList(
                                        SyntaxFactory.Token(SyntaxKind.PublicKeyword))),
                            new SyntaxList<InheritsStatementSyntax>(),
                            new SyntaxList<ImplementsStatementSyntax>(),
                            SyntaxFactory.SingletonList(method),
                            SyntaxFactory.EndClassStatement()
                        );

                        yield return partialClass;
                    }
                }
            }
        }

        private static StatementSyntax GetMembers(DatasetDefinition datasetDefinition)
        {
            return SyntaxFactory.PropertyBlock(
                SyntaxFactory.PropertyStatement(Identifier($"{datasetDefinition.Name}Dataset"))
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .WithAsClause(SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName(Identifier(datasetDefinition.Name)))),
                new SyntaxList<AccessorBlockSyntax>()
                {
                    SyntaxFactory.GetAccessorBlock(SyntaxFactory.GetAccessorStatement()),
                    SyntaxFactory.SetAccessorBlock(SyntaxFactory.SetAccessorStatement())
                },
                SyntaxFactory.EndPropertyStatement());
        }

    }
}
