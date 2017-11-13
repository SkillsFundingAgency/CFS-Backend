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
                        ClassDeclaration(Identifier($"{datasetDefinition.Name}Dataset"))
                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.PublicKeyword)))
                            .WithMembers(
                                List<MemberDeclarationSyntax>(GetMembers(datasetDefinition)
                                    ))))
                .NormalizeWhitespace();
        }

        private static IEnumerable<MemberDeclarationSyntax> GetMembers(DatasetDefinition datasetDefinition)
        {
            yield return CreateStaticDefinitionName(datasetDefinition);
            foreach (var memberDeclarationSyntax in GetStandardFields()) yield return memberDeclarationSyntax;
            foreach (var member in datasetDefinition.FieldDefinitions.Select(GetMember))
            {
                yield return member;
            }
        }

        private static IEnumerable<MemberDeclarationSyntax> GetStandardFields()
        {
            yield return GetMember(new DatasetFieldDefinition {Type = TypeCode.String, Name = "Id"});
            yield return GetMember(new DatasetFieldDefinition {Type = TypeCode.String, Name = "BudgetId"});
            yield return GetMember(new DatasetFieldDefinition {Type = TypeCode.String, Name = "ProviderUrn"});
            yield return GetMember(new DatasetFieldDefinition {Type = TypeCode.String, Name = "ProviderName"});
            yield return GetMember(new DatasetFieldDefinition {Type = TypeCode.String, Name = "DatasetName"});
        }

        private static MemberDeclarationSyntax CreateStaticDefinitionName(DatasetDefinition datasetDefinition)
        {
            return FieldDeclaration(
                    VariableDeclaration(
                            PredefinedType(
                                Token(SyntaxKind.StringKeyword)))
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator(
                                        Identifier("DatasetDefinitionName"))
                                    .WithInitializer(
                                        EqualsValueClause(
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(datasetDefinition.Name)))))))
                .WithModifiers(
                    TokenList(
                        new[]
                        {
                            Token(SyntaxKind.PublicKeyword),
                            Token(SyntaxKind.StaticKeyword)
                        }));
        }

        private static MemberDeclarationSyntax GetMember(DatasetFieldDefinition fieldDefinition)
        {
            var propertyType = GetType(fieldDefinition.Type);
            return PropertyDeclaration(
                    propertyType,
                    Identifier(Identifier(fieldDefinition.Name)))
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

    }
}
