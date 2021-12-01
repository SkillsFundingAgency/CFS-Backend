using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Calcs.ObsoleteItems;
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

        public IEnumerable<StatementSyntax> GetMembers(DatasetRelationshipSummary dataset,
            IEnumerable<ObsoleteItem> obsoleteItems)
        {
            DatasetDefinition datasetDefinition = dataset.DatasetDefinition;
            DatasetRelationshipType relationshipType = dataset.RelationshipType;
            PublishedSpecificationConfiguration publishedSpecificationConfiguration = dataset.PublishedSpecificationConfiguration;

            string datasourceName = dataset.RelationshipType == DatasetRelationshipType.ReleasedData ?
                dataset.Name :
                dataset.DatasetDefinition.Name;

            IList<StatementSyntax> members = new List<StatementSyntax>
            {
                dataset.RelationshipType == DatasetRelationshipType.ReleasedData ?
                    CreateStaticProperty("DatasetRelationshipName", dataset.Name) :
                    CreateStaticProperty("DatasetDefinitionName", dataset.DatasetDefinition.Name),
                dataset.RelationshipType == DatasetRelationshipType.ReleasedData ?
                    CreateStaticProperty("DatasetRelationshipId", dataset.Id) :
                    CreateStaticProperty("DatasetDefinitionId", dataset.DatasetDefinition.Id)
            };

            if (relationshipType == DatasetRelationshipType.Uploaded)
            {
                foreach (StatementSyntax member in datasetDefinition.TableDefinitions.First().FieldDefinitions.Select(_ => GetMember(_)))
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
                    },
                    item.IsObsolete || obsoleteItems.AnyWithNullCheck(_ => _.DatasetFieldId == item.TemplateId.ToString() && _.ItemType == ObsoleteItemType.DatasetField)));
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
                    },
                    item.IsObsolete || obsoleteItems.AnyWithNullCheck(_ => _.DatasetFieldId == item.TemplateId.ToString() && _.ItemType == ObsoleteItemType.DatasetField)));
                }
            }

            members.Add(GetHasValue());

            return members;
        }

        private static StatementSyntax CreateStaticProperty(string propertyName, string value)
        {
            SyntaxToken token = SyntaxFactory.Literal(value);
            VariableDeclaratorSyntax variable = SyntaxFactory.VariableDeclarator(
                SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier(propertyName)));
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

        private StatementSyntax GetMember(FieldDefinition fieldDefinition, bool isObsolete = false)
        {
            TypeSyntax propertyType = GetType(fieldDefinition.Type);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"<Field(Id := \"{fieldDefinition.Id}\", Name := \"{fieldDefinition.Name}\")>");
            builder.AppendLine($"<IsAggregable(IsAggregable := \"{fieldDefinition.IsAggregable}\")>");
            if (isObsolete)
            { 
                builder.AppendLine("<ObsoleteItem()>");
            }
            builder.AppendLine($"<Description(Description := \"{fieldDefinition.Description?.Replace("\"", "\"\"")}\")>");
            builder.AppendLine($"Public Property {_typeIdentifierGenerator.EscapeReservedWord(_typeIdentifierGenerator.GenerateIdentifier(fieldDefinition.Name))}() As {_typeIdentifierGenerator.GenerateIdentifier($"{propertyType}")}");
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());
            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                .FirstOrDefault();
        }
    }
}
