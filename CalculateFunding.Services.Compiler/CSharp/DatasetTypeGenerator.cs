using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CalculateFunding.Services.Compiler.CSharp
{

    public class DatasetTypeGenerator : CSharpTypeGenerator
    {
        public IEnumerable<SourceFile> GenerateDataset(Implementation budget)
        {
            foreach (var dataset in budget.DatasetDefinitions)
            {
                var @class = SyntaxFactory.ClassDeclaration(Identifier($"{dataset.Name}Dataset"))
                    .WithModifiers(
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .WithMembers(
                        SyntaxFactory.List(GetMembers(dataset)
                        )
                    );

                var syntaxTree =  SyntaxFactory.CompilationUnit()
                    .WithUsings(StandardUsings())
                    .WithMembers(
                        SyntaxFactory.SingletonList<MemberDeclarationSyntax>(@class))
                    .NormalizeWhitespace();

                yield return new SourceFile { FileName = $"{Identifier($"{dataset.Name}Dataset")}.cs", SourceCode = syntaxTree.ToFullString() };
            }

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
            yield return GetMember(new DatasetFieldDefinition {Type = FieldType.String, Name = "Id"});
            yield return GetMember(new DatasetFieldDefinition {Type = FieldType.String, Name = "BudgetId"});
            yield return GetMember(new DatasetFieldDefinition {Type = FieldType.String, Name = "ProviderUrn"});
            yield return GetMember(new DatasetFieldDefinition {Type = FieldType.String, Name = "ProviderName"});
            yield return GetMember(new DatasetFieldDefinition {Type = FieldType.String, Name = "DatasetName"});
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

        private static MemberDeclarationSyntax GetMember(DatasetFieldDefinition fieldDefinition)
        {
            var propertyType = GetType(fieldDefinition.Type);
            return SyntaxFactory.PropertyDeclaration(
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
