using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Allocations.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Allocations.Services.Calculator
{
    public class ProductTypeGenerator : CSharpTypeGenerator
    {
        public CompilationUnitSyntax GenerateCalcs(Budget budget)
        {
            return CompilationUnit()
                .WithUsings(StandardUsings())
                .WithMembers(
                    List<MemberDeclarationSyntax>(
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
                    yield return ClassDeclaration(Identifier("ProductCalculations"))
                        .WithModifiers(
                            TokenList(
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.PartialKeyword)))
                        .WithMembers(
                            List<MemberDeclarationSyntax>(budget.DatasetDefinitions.Select(GetMembers)
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
                var tree = ParseSyntaxTree(product.Calculation.SourceCode);

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
                    var partialClass = ClassDeclaration(Identifier("ProductCalculations"))

                        .WithModifiers(
                            TokenList(
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.PartialKeyword)))
                        .WithMembers(
                            SingletonList<MemberDeclarationSyntax>(GetMethod(product)));
                    if (partialClass == null)
                    {
                        partialClass = ClassDeclaration(Identifier("ProductCalculations"))

                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.PublicKeyword),
                                    Token(SyntaxKind.PartialKeyword)))
                            .WithMembers(
                                SingletonList<MemberDeclarationSyntax>(MethodDeclaration(
                                        PredefinedType(Token(SyntaxKind.DecimalKeyword)),
                                        Identifier(Identifier(product.Name)))
                                    .WithModifiers(
                                        TokenList(
                                            Token(SyntaxKind.PublicKeyword)))
                                    .WithBody(
                                        Block(
                                            SingletonList<StatementSyntax>(
                                                ThrowStatement(
                                                    ObjectCreationExpression(
                                                            IdentifierName("NotImplementedException"))
                                                        .WithArgumentList(
                                                            ArgumentList(
                                                                SingletonSeparatedList(
                                                                    Argument(
                                                                        LiteralExpression(
                                                                            SyntaxKind.StringLiteralExpression,
                                                                            Literal(
                                                                                $"{product.Name} is not implemented"))))))))))
                            ));


 
                    }
                    yield return partialClass;

                  
                }
            }
        }

        private static PropertyDeclarationSyntax GetMembers(DatasetDefinition datasetDefinition)
        {
            return PropertyDeclaration(
                    IdentifierName(Identifier($"{datasetDefinition.Name}Dataset")), Identifier(datasetDefinition.Name))
                 .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithAccessorList(
                    AccessorList(
                        List(
                            new[]
                            {
                                AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration)
                                    .WithSemicolonToken(
                                        Token(SyntaxKind.SemicolonToken)),
                                AccessorDeclaration(
                                        SyntaxKind.SetAccessorDeclaration)
                                    .WithSemicolonToken(
                                        Token(SyntaxKind.SemicolonToken))
                            })));
        }

        private static SyntaxList<AttributeListSyntax> ClassAttributes(string modelName)
        {
            return SingletonList(
                AttributeList(
                    SingletonSeparatedList(
                        Attribute(
                                IdentifierName("Allocation"))
                            .WithArgumentList(
                                AttributeArgumentList(
                                    SeparatedList<AttributeArgumentSyntax>(
                                        new SyntaxNodeOrToken[]
                                        {
                                            AttributeArgument(
                                                LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    Literal(modelName)))
                                        }))))));
        }
    }
}
