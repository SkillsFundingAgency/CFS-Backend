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
        public CompilationUnitSyntax GenerateDatasets(Budget budget)
        {

            var classes = budget.DatasetDefinitions.Select(x => SyntaxFactory.ClassBlock(
                SyntaxFactory.ClassStatement(
                        Identifier($"{x.Name}Dataset")
                    )
                    .WithModifiers(
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword))),
                new SyntaxList<InheritsStatementSyntax>(),
                new SyntaxList<ImplementsStatementSyntax>(),
                SyntaxFactory.List(GetMembers(x)),
                SyntaxFactory.EndClassStatement()


            ));

            return SyntaxFactory.CompilationUnit()
                .WithImports(StandardImports())
                .WithMembers(
                    SyntaxFactory.List<StatementSyntax>(classes))
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

        private static IEnumerable<StatementSyntax> GetStandardFields()
        {
            yield return GetMember(new DatasetFieldDefinition {Type = TypeCode.String, Name = "Id"});
            yield return GetMember(new DatasetFieldDefinition {Type = TypeCode.String, Name = "BudgetId"});
            yield return GetMember(new DatasetFieldDefinition {Type = TypeCode.String, Name = "ProviderUrn"});
            yield return GetMember(new DatasetFieldDefinition {Type = TypeCode.String, Name = "ProviderName"});
            yield return GetMember(new DatasetFieldDefinition {Type = TypeCode.String, Name = "DatasetName"});
        }

        private static StatementSyntax CreateStaticDefinitionName(DatasetDefinition datasetDefinition)
        {
            var token = SyntaxFactory.Literal(datasetDefinition.Name);
            var variable = SyntaxFactory.VariableDeclarator(
                SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier("DatasetDefinitionName")));
            variable = variable.WithAsClause(
                SyntaxFactory.SimpleAsClause(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))));

            variable = variable.WithInitializer(
                SyntaxFactory.EqualsValue(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                    token)));
            

            return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.SharedKeyword)),
                SyntaxFactory.SingletonSeparatedList(variable));
        }

        private static StatementSyntax GetMember(DatasetFieldDefinition fieldDefinition)
        {
            var propertyType = GetType(fieldDefinition.Type);
            return SyntaxFactory.PropertyStatement(Identifier(fieldDefinition.Name))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAsClause(SyntaxFactory.SimpleAsClause(propertyType));
        }

    }
}
