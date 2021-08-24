using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Calcs.ObsoleteItems;
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

        public IEnumerable<SourceFile> GenerateDatasetSourceFiles(BuildProject buildProject,
            IEnumerable<ObsoleteItem> obsoleteItems)
        {
            ClassBlockSyntax wrapperSyntaxTree = SyntaxFactory.ClassBlock(SyntaxFactory.ClassStatement("Datasets")
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))));

            if (buildProject.DatasetRelationships != null)
            {
                HashSet<string> typesCreated = new HashSet<string>();
                foreach (DatasetRelationshipSummary dataset in buildProject.DatasetRelationships)
                {
                    string datasourceName = dataset.RelationshipType == Models.Datasets.DatasetRelationshipType.ReleasedData ?
                        dataset.TargetSpecificationName :
                        dataset.DatasetDefinition.Name;

                    if (!typesCreated.Contains(datasourceName))
                    {
                        ClassBlockSyntax @class = SyntaxFactory.ClassBlock(
                            SyntaxFactory.ClassStatement(
                                    $"{_typeIdentifierGenerator.GenerateIdentifier(datasourceName)}Dataset"
                                )
                                .WithModifiers(
                                    SyntaxFactory.TokenList(
                                        SyntaxFactory.Token(SyntaxKind.PublicKeyword))),
                            new SyntaxList<InheritsStatementSyntax>(),
                            new SyntaxList<ImplementsStatementSyntax>(),
                            SyntaxFactory.List(_datasetTypeMemberGenerator.GetMembers(dataset, obsoleteItems)),
                            SyntaxFactory.EndClassStatement()
                        );

                        CompilationUnitSyntax syntaxTree = SyntaxFactory.CompilationUnit()
                            .WithImports(StandardImports())
                            .WithMembers(
                                SyntaxFactory.SingletonList<StatementSyntax>(@class))
                            .NormalizeWhitespace();
                        yield return new SourceFile { FileName = $"Datasets/{_typeIdentifierGenerator.GenerateIdentifier(datasourceName)}.vb", SourceCode = syntaxTree.ToFullString() };
                        typesCreated.Add(datasourceName);
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

            (string datasourceVariableName, string datasourceName, string datasourceId, string datasourceDescription) = datasetRelationship.RelationshipType == Models.Datasets.DatasetRelationshipType.ReleasedData ?
                (datasetRelationship.TargetSpecificationName, datasetRelationship.TargetSpecificationName, datasetRelationship.PublishedSpecificationConfiguration.SpecificationId, null) :
                (datasetRelationship.Name, datasetRelationship.DatasetDefinition.Name, datasetRelationship.DatasetDefinition.Id, datasetRelationship.DatasetDefinition.Description);

            builder.AppendLine($"<Field(Id := \"{datasourceId}\", Name := \"{datasourceName}\")>");
            if (!string.IsNullOrWhiteSpace(datasourceDescription))
            {
                builder.AppendLine($"<Description(Description := \"{datasourceDescription?.Replace("\"", "\"\"")}\")>");
            }

            builder.AppendLine(datasetRelationship.DataGranularity == DataGranularity.SingleRowPerProvider
                ? $"Public Property {_typeIdentifierGenerator.GenerateIdentifier(datasourceVariableName)}() As {_typeIdentifierGenerator.GenerateIdentifier($"{datasourceName}Dataset")}"
                : $"Public Property {_typeIdentifierGenerator.GenerateIdentifier(datasourceVariableName)}() As System.Collections.Generic.List(Of {_typeIdentifierGenerator.GenerateIdentifier($"{datasourceName}Dataset")})");

            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());
            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                .FirstOrDefault();
        }
    }
}
