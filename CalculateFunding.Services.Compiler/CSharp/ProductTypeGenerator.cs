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
        public IEnumerable<SourceFile> GenerateCalcs(Implementation budget)
        {
            var syntaxTree = SyntaxFactory.CompilationUnit()
                .WithUsings(StandardUsings())
                .WithMembers(
                    SyntaxFactory.List<MemberDeclarationSyntax>(
                        Classes(budget)
                        ))
                .NormalizeWhitespace();

            yield return new SourceFile{FileName = "ProductCalculations.cs", SourceCode = syntaxTree.ToFullString()};
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

        private static MethodDeclarationSyntax GetMethod(Calculation product)
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

        private static IEnumerable<ClassDeclarationSyntax> GetProductPartials(Calculation calc)
        {
                    var partialClass = SyntaxFactory.ClassDeclaration(Identifier("ProductCalculations"))

                        .WithModifiers(
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                        .WithMembers(
                            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(GetMethod(calc).WithAttributeLists(GetMethodAttributes(calc))));
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
                                    .WithAttributeLists(GetMethodAttributes(calc))
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

        private static SyntaxList<AttributeListSyntax> GetMethodAttributes(Calculation calc)
        {
            return SyntaxFactory.SingletonList(SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(
                        SyntaxFactory.ParseName("Display"),
                        SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(new []
                        {
                            SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals("ShortName"), SyntaxFactory.NameColon("what"), SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(calc.Id))),
                            SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals("Name"), SyntaxFactory.NameColon("what"), SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(calc.Name))),
                            SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals("Description"), SyntaxFactory.NameColon("what"), SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(calc.Name)))

                        })))
                        )));
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
