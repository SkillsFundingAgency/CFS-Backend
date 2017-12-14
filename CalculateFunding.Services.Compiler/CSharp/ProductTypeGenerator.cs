using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CalculateFunding.Services.Compiler.CSharp
{
    public class ProductTypeGenerator : CSharpTypeGenerator
    {
        public CompilationUnitSyntax GenerateCalcs(Implementation budget)
        {
            return SyntaxFactory.CompilationUnit()
                .WithUsings(StandardUsings())
                .WithMembers(
                    SyntaxFactory.List<MemberDeclarationSyntax>(
                        Classes(budget)
                        ))
                .NormalizeWhitespace();
        }

        private static IEnumerable<ClassDeclarationSyntax> Classes(Implementation budget)
        {
            foreach (var calculation in budget.Calculations)
            {
                yield return SyntaxFactory.ClassDeclaration(Identifier("ProductCalculations"))
                    .WithModifiers(
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                            SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                    .WithMembers(
                        SyntaxFactory.List<MemberDeclarationSyntax>(budget.DatasetDefinitions.Select(GetMembers)
                        ));

                foreach (var partialClass in GetProductPartials(calculation))
                {
                    yield return partialClass;
                }
            }
        }

        private static MethodDeclarationSyntax GetMethod(CalculationImplementation product)
        {
            if (product?.SourceCode != null)
            {
                var tree = SyntaxFactory.ParseSyntaxTree(product.SourceCode);

                var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault();


                return method;
            }
            return null;
        }

        private static IEnumerable<ClassDeclarationSyntax> GetProductPartials(CalculationImplementation calc)
        {
                    var partialClass = SyntaxFactory.ClassDeclaration(Identifier("ProductCalculations"))

                        .WithModifiers(
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                        .WithMembers(
                            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(GetMethod(calc)));
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
                                        Identifier(Identifier(calc.Name)))
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
                                                                                $"{calc.Name} is not implemented"))))))))))
                            ));


 
                    }
                    yield return partialClass;

                  
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
