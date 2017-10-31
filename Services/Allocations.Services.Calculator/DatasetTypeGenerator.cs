using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Allocations.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Allocations.Services.Calculator
{

    public class DatasetTypeGenerator : CSharpTypeGenerator
    {
        public CompilationUnitSyntax Test(Budget budget, DatasetDefinition datasetDefinition)
        {
            return CompilationUnit()
                .WithUsings(StandardUsings())
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        ClassDeclaration(Identifier(datasetDefinition.Name))
                            .WithAttributeLists(
                                ClassAttributes(budget.Name, datasetDefinition.Name))
                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.PublicKeyword)))
                            .WithBaseList(
                                BaseList(
                                    SingletonSeparatedList<BaseTypeSyntax>(
                                        SimpleBaseType(
                                            IdentifierName("ProviderSourceDataset")))))
                            .WithMembers(
                                List<MemberDeclarationSyntax>(datasetDefinition.FieldDefinitions.Select(GetMember)
                                    ))))
                .NormalizeWhitespace();
        }

        private static PropertyDeclarationSyntax GetMember(DatasetFieldDefinition fieldDefinition)
        {
            var propertyType = GetType(fieldDefinition.Type);
            return PropertyDeclaration(
                    propertyType,
                    Identifier(Identifier(fieldDefinition.Name)))
                .WithAttributeLists(
                    List(PropertyAttributes(fieldDefinition.LongName, IdentifierCamelCase(fieldDefinition.Name))))
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


        private static IEnumerable<AttributeListSyntax> PropertyAttributes(string description, string jsonIdentifier)
        {
            if (!string.IsNullOrWhiteSpace(description))
            {
                yield return AttributeList(
                    SingletonSeparatedList(
                        Attribute(
                                IdentifierName("Description"))
                            .WithArgumentList(
                                AttributeArgumentList(
                                    SingletonSeparatedList(
                                        AttributeArgument(
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(description))))))));
            }

            yield return AttributeList(
                SingletonSeparatedList(
                    Attribute(
                            IdentifierName("JsonProperty"))
                        .WithArgumentList(
                            AttributeArgumentList(
                                SingletonSeparatedList(
                                    AttributeArgument(
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal(jsonIdentifier))))))));

        }

        private static SyntaxList<AttributeListSyntax> ClassAttributes(string modelName, string description)
        {
            return SingletonList(
                AttributeList(
                    SingletonSeparatedList(
                        Attribute(
                                IdentifierName("Dataset"))
                            .WithArgumentList(
                                AttributeArgumentList(
                                    SeparatedList<AttributeArgumentSyntax>(
                                        new SyntaxNodeOrToken[]
                                        {
                                            AttributeArgument(
                                                LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    Literal(modelName))),
                                            Token(SyntaxKind.CommaToken),
                                            AttributeArgument(
                                                LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    Literal(description)))
                                        }))))));
        }
    }
}
