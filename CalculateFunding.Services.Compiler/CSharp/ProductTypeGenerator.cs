using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Allocations.Services.Compiler.CSharp
{
    public class ProductTypeGenerator : CSharpTypeGenerator
    {
        public CompilationUnitSyntax GenerateCalcs(Budget budget)
        {
            return SyntaxFactory.CompilationUnit()
                .WithUsings(StandardUsings())
                .WithMembers(
                    SyntaxFactory.List<MemberDeclarationSyntax>(
                        Classes(budget)
                        ))
                .NormalizeWhitespace();
        }

        private static IEnumerable<ClassDeclarationSyntax> Classes(Budget budget)
        {
            foreach (var fundingPolicy in budget.FundingPolicies)
            {
                foreach (var allocationLine in fundingPolicy.AllocationLines)
                {
                    yield return SyntaxFactory.ClassDeclaration(Identifier("ProductCalculations"))
                        .WithModifiers(
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                        .WithMembers(
                            SyntaxFactory.List<MemberDeclarationSyntax>(budget.DatasetDefinitions.Select(GetMembers)
                            ));

                        foreach (var partialClass in GetProductPartials(allocationLine))
                    {
                        yield return partialClass;
                    }
                }
            }
        }

        private static MethodDeclarationSyntax GetMethod(Product product)
        {
            if (product.Calculation?.SourceCode != null)
            {
                var tree = SyntaxFactory.ParseSyntaxTree(product.Calculation.SourceCode);

                var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault();


                return method;
            }
            return null;
        }

        private static IEnumerable<ClassDeclarationSyntax> GetProductPartials(AllocationLine allocationLine)
        {
            foreach (var productFolder in allocationLine.ProductFolders)
            {
                foreach (var product in productFolder.Products)
                {
                    var partialClass = SyntaxFactory.ClassDeclaration(Identifier("ProductCalculations"))

                        .WithModifiers(
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                        .WithMembers(
                            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(GetMethod(product)));
                    if (partialClass == null)
                    {
                        partialClass = SyntaxFactory.ClassDeclaration(Identifier("ProductCalculations"))

                            .WithModifiers(
                                SyntaxFactory.TokenList(
                                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                    SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                            .WithMembers(
                                SyntaxFactory.SingletonList<MemberDeclarationSyntax>(SyntaxFactory.MethodDeclaration(
                                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DecimalKeyword)),
                                        Identifier(Identifier(product.Name)))
                                    .WithModifiers(
                                        SyntaxFactory.TokenList(
                                            SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                    .WithBody(
                                        SyntaxFactory.Block(
                                            SyntaxFactory.SingletonList<StatementSyntax>(
                                                SyntaxFactory.ThrowStatement(
                                                    SyntaxFactory.ObjectCreationExpression(
                                                            SyntaxFactory.IdentifierName("NotImplementedException"))
                                                        .WithArgumentList(
                                                            SyntaxFactory.ArgumentList(
                                                                SyntaxFactory.SingletonSeparatedList(
                                                                    SyntaxFactory.Argument(
                                                                        SyntaxFactory.LiteralExpression(
                                                                            SyntaxKind.StringLiteralExpression,
                                                                            SyntaxFactory.Literal(
                                                                                $"{product.Name} is not implemented"))))))))))
                            ));


 
                    }
                    yield return partialClass;

                  
                }
            }
        }

        private static PropertyDeclarationSyntax GetMembers(DatasetDefinition datasetDefinition)
        {
            return SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.IdentifierName(Identifier($"{datasetDefinition.Name}Dataset")), Identifier(datasetDefinition.Name))
                 .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAccessorList(
                    SyntaxFactory.AccessorList(
                        SyntaxFactory.List(
                            new[]
                            {
                                SyntaxFactory.AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration)
                                    .WithSemicolonToken(
                                        SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                SyntaxFactory.AccessorDeclaration(
                                        SyntaxKind.SetAccessorDeclaration)
                                    .WithSemicolonToken(
                                        SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            })));
        }

    }
}
