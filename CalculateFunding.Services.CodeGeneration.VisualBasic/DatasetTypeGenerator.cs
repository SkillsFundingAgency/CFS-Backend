using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Schema;
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

			if (budget.DatasetRelationships != null)
	        {
                var typesCreated = new HashSet<string>();
				foreach (var dataset in budget.DatasetRelationships)
				{
				    if (!typesCreated.Contains(dataset.Value.Name))
				    {
				        var @class = SyntaxFactory.ClassBlock(
				            SyntaxFactory.ClassStatement(
				                    $"{Identifier(dataset.Value.Name)}Dataset"
				                )
				                .WithModifiers(
				                    SyntaxFactory.TokenList(
				                        SyntaxFactory.Token(SyntaxKind.PublicKeyword))),
				            new SyntaxList<InheritsStatementSyntax>(),
				            new SyntaxList<ImplementsStatementSyntax>(),
				            SyntaxFactory.List(GetMembers(dataset.Value)),
				            SyntaxFactory.EndClassStatement()

				        );

				        var syntaxTree = SyntaxFactory.CompilationUnit()
				            .WithImports(StandardImports())
				            .WithMembers(
				                SyntaxFactory.SingletonList<StatementSyntax>(@class))
				            .NormalizeWhitespace();
				        yield return new SourceFile { FileName = $"Datasets/{Identifier(dataset.Value.Name)}.vb", SourceCode = syntaxTree.ToFullString() };
				        typesCreated.Add(dataset.Value.Name);
				    }


					wrapperSyntaxTree =
						wrapperSyntaxTree.WithMembers(SyntaxFactory.List(budget.DatasetRelationships.Select(x => GetDatasetProperties(x.Key, x.Value))));
				}
			}



            yield return new SourceFile { FileName = $"Datasets/Datasets.vb", SourceCode = wrapperSyntaxTree.NormalizeWhitespace().ToFullString() };


        }

        private static IEnumerable<StatementSyntax> GetMembers(DatasetDefinition datasetDefinition)
        {
            yield return CreateStaticDefinitionName(datasetDefinition);
            foreach (var member in datasetDefinition.TableDefinitions.First().FieldDefinitions.Select(GetMember))
            {
                yield return member;
            }
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

        private static StatementSyntax GetMember(FieldDefinition fieldDefinition)
        {
            var propertyType = GetType(fieldDefinition.Type);
            var builder = new StringBuilder();
            builder.AppendLine($"<Field(Id := \"{fieldDefinition.Id}\", Name := \"{fieldDefinition.Name}\")>");
            builder.AppendLine($"Public Property {Identifier(fieldDefinition.Name)}() As {Identifier($"{propertyType}")}");
            var tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());
            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                .FirstOrDefault();
        }

        private static StatementSyntax GetDatasetProperties(string name, DatasetDefinition datasetDefinition)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"<DatasetRelationship(Name := \"{name}\")>");
            builder.AppendLine($"Public Property {Identifier(name)}() As {Identifier($"{datasetDefinition.Name}Dataset")}");
            var tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());
            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                .FirstOrDefault();
        }

    }
}
