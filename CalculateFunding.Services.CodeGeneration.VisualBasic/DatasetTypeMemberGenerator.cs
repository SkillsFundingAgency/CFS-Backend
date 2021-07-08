using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class DatasetTypeMemberGenerator : VisualBasicTypeGenerator
    {
        private readonly ITypeIdentifierGenerator _typeIdentifierGenerator;

        public DatasetTypeMemberGenerator(ITypeIdentifierGenerator typeIdentifierGenerator)
        {
            _typeIdentifierGenerator = new VisualBasicTypeIdentifierGenerator();
        }

        public IEnumerable<StatementSyntax> GetMembers(DatasetRelationshipSummary dataset)
        {
            DatasetDefinition datasetDefinition = dataset.DatasetDefinition;
            DatasetRelationshipType relationshipType = dataset.RelationshipType;
            PublishedSpecificationConfiguration publishedSpecificationConfiguration = dataset.PublishedSpecificationConfiguration;

            IList<StatementSyntax> members = new List<StatementSyntax>
            {
                CreateStaticDefinitionName(datasetDefinition),
                CreateStaticDefinitionId(datasetDefinition)
            };

            if (relationshipType == DatasetRelationshipType.Uploaded)
            {
                foreach (StatementSyntax member in datasetDefinition.TableDefinitions.First().FieldDefinitions.Select(GetMember))
                {
                    members.Add(member);
                }
            }
            else if (relationshipType == DatasetRelationshipType.ReleasedData)
            {
                var calculationPrefix = CodeGenerationDatasetTypeConstants.CalculationPrefix;
                var fundingLinePrefix = CodeGenerationDatasetTypeConstants.FundingLinePrefix;

                members.Add(GetMember(new FieldDefinition()
                {
                    Name = CodeGenerationDatasetTypeConstants.UKPRNFieldName,
                    Id = CodeGenerationDatasetTypeConstants.UKPRNFieldName,
                    Description = CodeGenerationDatasetTypeConstants.UKPRNFieldName,
                    IsAggregable = false,
                    Required = true,
                    IdentifierFieldType = IdentifierFieldType.UKPRN,
                    Type = FieldType.String,
                }));

                foreach (PublishedSpecificationItem item in publishedSpecificationConfiguration.Calculations)
                {
                    members.Add(GetMember(new FieldDefinition()
                    {
                        Name = $"{calculationPrefix}_{item.TemplateId}_{item.Name}",
                        Id = $"{calculationPrefix}_{item.TemplateId}",
                        Description = item.Name,
                        Type = item.FieldType,
                        IsAggregable = false
                    }));
                }

                foreach (PublishedSpecificationItem item in publishedSpecificationConfiguration.FundingLines)
                {
                    members.Add(GetMember(new FieldDefinition()
                    {
                        Name = $"{fundingLinePrefix}_{item.TemplateId}_{item.Name}",
                        Id = $"{fundingLinePrefix}_{item.TemplateId}",
                        Description = item.Name,
                        Type = item.FieldType,
                        IsAggregable = false
                    }));
                }
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

        private static StatementSyntax GetHasValue()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"<Description(Description := \"Return whether the dataset exists for the current provider.\")>");
            builder.AppendLine("Public Property HasValue As Boolean");

            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());
            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                .FirstOrDefault();
        }

        private StatementSyntax GetMember(FieldDefinition fieldDefinition)
        {
            TypeSyntax propertyType = GetType(fieldDefinition.Type);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"<Field(Id := \"{fieldDefinition.Id}\", Name := \"{fieldDefinition.Name}\")>");
            builder.AppendLine($"<IsAggregable(IsAggregable := \"{fieldDefinition.IsAggregable.ToString()}\")>");
            builder.AppendLine($"<Description(Description := \"{fieldDefinition.Description?.Replace("\"", "\"\"")}\")>");
            builder.AppendLine($"Public Property {_typeIdentifierGenerator.GenerateIdentifier(fieldDefinition.Name)}() As {_typeIdentifierGenerator.GenerateIdentifier($"{propertyType}")}");
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());
            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                .FirstOrDefault();
        }
    }
}
