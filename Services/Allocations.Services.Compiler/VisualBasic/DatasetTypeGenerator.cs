using System;
using System.Collections.Generic;
using System.Linq;
using Allocations.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;


namespace Allocations.Services.Compiler.VisualBasic
{

    public class DatasetTypeGenerator : VisualBasicTypeGenerator
    {
        public CompilationUnitSyntax GenerateDataset(Budget budget, DatasetDefinition datasetDefinition)
        {
            return SyntaxFactory.CompilationUnit()
                .WithImports(StandardImports())
                .WithMembers(
                    SyntaxFactory.SingletonList<StatementSyntax>(
                        SyntaxFactory.ClassBlock(
                                SyntaxFactory.ClassStatement(
                                        Identifier($"{datasetDefinition.Name}Dataset")
                                    )
                                    .WithModifiers(
                                        SyntaxFactory.TokenList(
                                            SyntaxFactory.Token(SyntaxKind.PublicKeyword))),
                                new SyntaxList<InheritsStatementSyntax>(), 
                                new SyntaxList<ImplementsStatementSyntax>(), 
                                new SyntaxList<StatementSyntax>(GetMembers(datasetDefinition)),
                                SyntaxFactory.EndClassStatement()
                            )
                                    ))
                .NormalizeWhitespace();

        }



        private static IEnumerable<StatementSyntax> GetMembers(DatasetDefinition datasetDefinition)
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
            return SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.PredefinedType(
                                SyntaxFactory.Token(SyntaxKind.StringKeyword)))
                        .WithVariables(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator(
                                        Identifier("DatasetDefinitionName"))
                                    .WithInitializer(
                                        SyntaxFactory.EqualsValueClause(
                                            SyntaxFactory.LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                SyntaxFactory.Literal(datasetDefinition.Name)))))))
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        new[]
                        {
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                            SyntaxFactory.Token(SyntaxKind.StaticKeyword)
                        }));
        }

        private static StatementSyntax GetMember(DatasetFieldDefinition fieldDefinition)
        {
            var propertyType = GetType(fieldDefinition.Type);
            return SyntaxFactory.PropertyBlock()(
                    propertyType,
                    Identifier(Identifier(fieldDefinition.Name)))
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
