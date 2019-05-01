using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{

    public class DatasetTypeGenerator : VisualBasicTypeGenerator
    {
        public IEnumerable<SourceFile> GenerateDatasets(BuildProject buildProject)
        {

            ClassBlockSyntax wrapperSyntaxTree = SyntaxFactory.ClassBlock(SyntaxFactory.ClassStatement("Datasets")
				.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))));

			if (buildProject.DatasetRelationships != null)
	        {
                HashSet<string> typesCreated = new HashSet<string>();
				foreach (DatasetRelationshipSummary dataset in buildProject.DatasetRelationships)
				{
				    if (!typesCreated.Contains(dataset.DatasetDefinition.Name))
				    {
                        ClassBlockSyntax @class = SyntaxFactory.ClassBlock(
				            SyntaxFactory.ClassStatement(
				                    $"{GenerateIdentifier(dataset.DatasetDefinition.Name)}Dataset"
				                )
				                .WithModifiers(
				                    SyntaxFactory.TokenList(
				                        SyntaxFactory.Token(SyntaxKind.PublicKeyword))),
				            new SyntaxList<InheritsStatementSyntax>(),
				            new SyntaxList<ImplementsStatementSyntax>(),
				            SyntaxFactory.List(GetMembers(dataset.DatasetDefinition)),
				            SyntaxFactory.EndClassStatement()
				        );

                        CompilationUnitSyntax syntaxTree = SyntaxFactory.CompilationUnit()
				            .WithImports(StandardImports())
				            .WithMembers(
				                SyntaxFactory.SingletonList<StatementSyntax>(@class))
				            .NormalizeWhitespace();
				        yield return new SourceFile { FileName = $"Datasets/{GenerateIdentifier(dataset.DatasetDefinition.Name)}.vb", SourceCode = syntaxTree.ToFullString() };
				        typesCreated.Add(dataset.DatasetDefinition.Name);
				    }


					wrapperSyntaxTree =
						wrapperSyntaxTree.WithMembers(SyntaxFactory.List(buildProject.DatasetRelationships.Select(GetDatasetProperties)));
				}
			}
            yield return new SourceFile { FileName = $"Datasets/Datasets.vb", SourceCode = wrapperSyntaxTree.NormalizeWhitespace().ToFullString() };
        }

        private static IEnumerable<StatementSyntax> GetMembers(DatasetDefinition datasetDefinition)
        {
            IList<StatementSyntax> members = new List<StatementSyntax>
            {
                CreateStaticDefinitionName(datasetDefinition),
                CreateStaticDefinitionId(datasetDefinition)
            };

            foreach (StatementSyntax member in datasetDefinition.TableDefinitions.First().FieldDefinitions.Select(GetMember))
            {
                members.Add(member);
            }

            members.Add(GetHasValue());

            return members;
        }

        private static StatementSyntax CreateStaticDefinitionName(DatasetDefinition datasetDefinition)
        {
            SyntaxToken token = SyntaxFactory.Literal(datasetDefinition.Name);
            VariableDeclaratorSyntax variable = SyntaxFactory.VariableDeclarator(
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

        private static StatementSyntax CreateStaticDefinitionId(DatasetDefinition datasetDefinition)
        {
            SyntaxToken token = SyntaxFactory.Literal(datasetDefinition.Id);
            VariableDeclaratorSyntax variable = SyntaxFactory.VariableDeclarator(
                SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier("DatasetDefinitionId")));
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
            TypeSyntax propertyType = GetType(fieldDefinition.Type);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"<Field(Id := \"{fieldDefinition.Id}\", Name := \"{fieldDefinition.Name}\")>");
            builder.AppendLine($"<IsAggregable(IsAggregable := \"{fieldDefinition.IsAggregable.ToString()}\")>");
            builder.AppendLine($"<Description(Description := \"{fieldDefinition.Description?.Replace("\"", "\"\"")}\")>");
            builder.AppendLine($"Public Property {GenerateIdentifier(fieldDefinition.Name)}() As {GenerateIdentifier($"{propertyType}")}");
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());
            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                .FirstOrDefault();
        }

        private static StatementSyntax GetHasValue()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"<Description(Description := \"Return whether the dataset exists for the current provider.\")>");
            builder.AppendLine("Public Property HasValue As Boolean");

            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());
            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                .FirstOrDefault();
        }

        private static StatementSyntax GetDatasetProperties(DatasetRelationshipSummary datasetRelationship)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"<DatasetRelationship(Id := \"{datasetRelationship.Id}\", Name := \"{datasetRelationship.Name}\")>");
            builder.AppendLine($"<Field(Id := \"{datasetRelationship.DatasetDefinition.Id}\", Name := \"{datasetRelationship.DatasetDefinition.Name}\")>");
            if (!string.IsNullOrWhiteSpace(datasetRelationship?.DatasetDefinition?.Description))
            {
                builder.AppendLine($"<Description(Description := \"{datasetRelationship.DatasetDefinition.Description?.Replace("\"", "\"\"")}\")>");
            }

            builder.AppendLine(datasetRelationship.DataGranularity == DataGranularity.SingleRowPerProvider
                ? $"Public Property {GenerateIdentifier(datasetRelationship.Name)}() As {GenerateIdentifier($"{datasetRelationship.DatasetDefinition.Name}Dataset")}"
                : $"Public Property {GenerateIdentifier(datasetRelationship.Name)}() As System.Collections.Generic.List(Of {GenerateIdentifier($"{datasetRelationship.DatasetDefinition.Name}Dataset")})");

            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());
            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                .FirstOrDefault();
        }

    }
}
