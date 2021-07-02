using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class DatasetTypeGenerator : VisualBasicTypeGenerator
    {
        private readonly ITypeIdentifierGenerator _typeIdentifierGenerator;
        private readonly DatasetTypeMemberGenerator _datasetTypeMemberGenerator;

        public DatasetTypeGenerator()
        {
            _typeIdentifierGenerator = new VisualBasicTypeIdentifierGenerator();
            _datasetTypeMemberGenerator = new DatasetTypeMemberGenerator(_typeIdentifierGenerator);
        }

        public IEnumerable<SourceFile> GenerateDatasetSourceFiles(BuildProject buildProject)
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
                                    $"{_typeIdentifierGenerator.GenerateIdentifier(dataset.DatasetDefinition.Name)}Dataset"
                                )
                                .WithModifiers(
                                    SyntaxFactory.TokenList(
                                        SyntaxFactory.Token(SyntaxKind.PublicKeyword))),
                            new SyntaxList<InheritsStatementSyntax>(),
                            new SyntaxList<ImplementsStatementSyntax>(),
                            SyntaxFactory.List(_datasetTypeMemberGenerator.GetMembers(dataset)),
                            SyntaxFactory.EndClassStatement()
                        );

                        CompilationUnitSyntax syntaxTree = SyntaxFactory.CompilationUnit()
                            .WithImports(StandardImports())
                            .WithMembers(
                                SyntaxFactory.SingletonList<StatementSyntax>(@class))
                            .NormalizeWhitespace();
                        yield return new SourceFile { FileName = $"Datasets/{_typeIdentifierGenerator.GenerateIdentifier(dataset.DatasetDefinition.Name)}.vb", SourceCode = syntaxTree.ToFullString() };
                        typesCreated.Add(dataset.DatasetDefinition.Name);
                    }


                    wrapperSyntaxTree =
                        wrapperSyntaxTree.WithMembers(SyntaxFactory.List(buildProject.DatasetRelationships.Select(GetDatasetProperties)));
                }
            }
            yield return new SourceFile { FileName = $"Datasets/Datasets.vb", SourceCode = wrapperSyntaxTree.NormalizeWhitespace().ToFullString() };
        }

        private StatementSyntax GetDatasetProperties(DatasetRelationshipSummary datasetRelationship)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"<DatasetRelationship(Id := \"{datasetRelationship.Id}\", Name := \"{datasetRelationship.Name}\")>");
            builder.AppendLine($"<Field(Id := \"{datasetRelationship.DatasetDefinition.Id}\", Name := \"{datasetRelationship.DatasetDefinition.Name}\")>");
            if (!string.IsNullOrWhiteSpace(datasetRelationship?.DatasetDefinition?.Description))
            {
                builder.AppendLine($"<Description(Description := \"{datasetRelationship.DatasetDefinition.Description?.Replace("\"", "\"\"")}\")>");
            }

            builder.AppendLine(datasetRelationship.DataGranularity == DataGranularity.SingleRowPerProvider
                ? $"Public Property {_typeIdentifierGenerator.GenerateIdentifier(datasetRelationship.Name)}() As {_typeIdentifierGenerator.GenerateIdentifier($"{datasetRelationship.DatasetDefinition.Name}Dataset")}"
                : $"Public Property {_typeIdentifierGenerator.GenerateIdentifier(datasetRelationship.Name)}() As System.Collections.Generic.List(Of {_typeIdentifierGenerator.GenerateIdentifier($"{datasetRelationship.DatasetDefinition.Name}Dataset")})");

            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());
            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                .FirstOrDefault();
        }
    }
}
