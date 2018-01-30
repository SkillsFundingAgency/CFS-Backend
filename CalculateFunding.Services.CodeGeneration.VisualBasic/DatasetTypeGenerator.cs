using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{

    public class DatasetTypeGenerator : VisualBasicTypeGenerator
    {
        public IEnumerable<SourceFile> GenerateDatasets(BuildProject budget)
        {

	        var wrapperSyntaxTree = SyntaxFactory.ClassBlock(SyntaxFactory.ClassStatement("Datasets")
				.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))));

			if (budget.DatasetDefinitions != null)
	        {
				foreach (var dataset in budget.DatasetDefinitions)
				{
					var @class = SyntaxFactory.ClassBlock(
						SyntaxFactory.ClassStatement(
								$"{Identifier(dataset.Name)}Dataset"
							)
							.WithModifiers(
								SyntaxFactory.TokenList(
									SyntaxFactory.Token(SyntaxKind.PublicKeyword))),
						new SyntaxList<InheritsStatementSyntax>(),
						new SyntaxList<ImplementsStatementSyntax>(),
						SyntaxFactory.List(GetMembers(dataset)),
						SyntaxFactory.EndClassStatement()

					);

					var syntaxTree = SyntaxFactory.CompilationUnit()
						.WithImports(StandardImports())
						.WithMembers(
							SyntaxFactory.SingletonList<StatementSyntax>(@class))
						.NormalizeWhitespace();
					yield return new SourceFile { FileName = $"Datasets/{Identifier(dataset.Name)}.vb", SourceCode = syntaxTree.ToFullString() };

					wrapperSyntaxTree =
						wrapperSyntaxTree.WithMembers(SyntaxFactory.List(budget.DatasetDefinitions.Select(GetDatasetProperties)));
				}
			}



            yield return new SourceFile { FileName = $"Datasets/Datasets.vb", SourceCode = wrapperSyntaxTree.NormalizeWhitespace().ToFullString() };


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
            yield return GetMember(new DatasetFieldDefinition {Type = FieldType.String, Name = "Id"});
            yield return GetMember(new DatasetFieldDefinition {Type = FieldType.String, Name = "BudgetId"});
            yield return GetMember(new DatasetFieldDefinition {Type = FieldType.String, Name = "ProviderUrn"});
            yield return GetMember(new DatasetFieldDefinition {Type = FieldType.String, Name = "ProviderName"});
            yield return GetMember(new DatasetFieldDefinition {Type = FieldType.String, Name = "DatasetName"});
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

        private static StatementSyntax GetDatasetProperties(DatasetDefinition datasetDefinition)
        {
            return SyntaxFactory.PropertyStatement(Identifier(datasetDefinition.Name))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAsClause(
                    SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName(Identifier($"{datasetDefinition.Name}Dataset"))));
        }

    }
}
