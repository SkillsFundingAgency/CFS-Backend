using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using CalculateFunding.Services.Core.Constants;
using FluentAssertions;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic.UnitTests
{
    [TestClass]
    public class DatasetTypeMemberGeneratorTests
    {
        private ITypeIdentifierGenerator typeIdentifierGenerator;

        [TestInitialize]
        public void Initialize()
        {
            typeIdentifierGenerator = Substitute.For<ITypeIdentifierGenerator>();
        }

        [TestMethod]
        public void GeneratesMembersForRelationshipTypeUploaded()
        {
            DatasetRelationshipSummary datasetRelationshipSummary = new DatasetRelationshipSummaryBuilder()
                .WithType(DatasetRelationshipType.Uploaded)
                .WithTableDefinitions(new List<TableDefinition>
                {
                    new TableDefinition {
                        Id = "Table1",
                        Name = "Table1",
                        Description = "Table1",
                        FieldDefinitions = new List<FieldDefinition>
                        {
                            new FieldDefinition { Id = "123", Name = "ID", Description = "ID", IsAggregable = false },
                            new FieldDefinition { Id = "TOTAL", Name = "TOTAL", Description = "TOTAL", IsAggregable = true },
                        }
                    }
                })
                .Build();

            DatasetTypeMemberGenerator sut = new DatasetTypeMemberGenerator(typeIdentifierGenerator);
            IEnumerable<StatementSyntax> result = sut.GetMembers(datasetRelationshipSummary, null);

            IEnumerable<PropertyStatementSyntax> properties = result.OfType<PropertyStatementSyntax>();
            properties.Should().HaveCount(3);

            string idProperty = properties.Where(s => s.Identifier.ValueText == "ID").First().ToFullString();
            idProperty.Should().Contain("Id := \"123\"");
            idProperty.Should().Contain("Name := \"ID\"");
            idProperty.Should().Contain("IsAggregable := \"False\"");

            string totalProperty = properties.Where(s => s.Identifier.ValueText == "TOTAL").First().ToFullString();
            totalProperty.Should().Contain("Id := \"TOTAL\"");
            totalProperty.Should().Contain("Name := \"TOTAL\"");
            totalProperty.Should().Contain("IsAggregable := \"True\"");

            string hasValueProperty = properties.Where(s => s.Identifier.ValueText == "HasValue").First().ToFullString();
            hasValueProperty.Should().Contain("Return whether the dataset exists for the current provider.");
            hasValueProperty.Should().Contain("Public Property HasValue As Boolean");
        }

        [TestMethod]
        public void GeneratesMembersForRelationshipTypeReleasedData()
        {
            string targetSpecificationId = "Target Specification Id";
            string datasetRelationshipId = "Dataset Relationship Id";
            string datasetRelationshipName = "Dataset Relationship Name";

            DatasetRelationshipSummary datasetRelationshipSummary = new DatasetRelationshipSummaryBuilder()
                .WithType(DatasetRelationshipType.ReleasedData)
                .WithTargetSpecificationId(targetSpecificationId)
                .WithDatasetRelationship(new Reference(datasetRelationshipId, datasetRelationshipName))
                .WithPublishedSpecificationConfigurationFundingLines(new List<PublishedSpecificationItem>
                {
                    new PublishedSpecificationItem
                    {
                        TemplateId = 1,
                        Name = "Funding Line 1",
                        SourceCodeName = "FL1",
                        FieldType = FieldType.String
                    },
                    new PublishedSpecificationItem
                    {
                        TemplateId = 3,
                        Name = "Funding Line 2",
                        SourceCodeName = "FL2",
                        FieldType = FieldType.String,
                        IsObsolete = true
                    },
                    new PublishedSpecificationItem
                    {
                        TemplateId = 4,
                        Name = "Funding Line 3",
                        SourceCodeName = "FL3",
                        FieldType = FieldType.String
                    }
                })
                .WithPublishedSpecificationConfigurationCalculations(new List<PublishedSpecificationItem>
                {
                    new PublishedSpecificationItem
                    {
                        TemplateId = 2,
                        Name = "Calculation 2",
                        SourceCodeName = "CALC2",
                        FieldType = FieldType.Integer
                    }
                })
                .Build();

            DatasetTypeMemberGenerator sut = new DatasetTypeMemberGenerator(typeIdentifierGenerator);
            IEnumerable<StatementSyntax> result = sut.GetMembers(datasetRelationshipSummary, new[] { 
                new ObsoleteItem
                {
                    DatasetFieldId = "4",
                    ItemType = ObsoleteItemType.DatasetField
                } 
            });

            IEnumerable<FieldDeclarationSyntax> fields = result.OfType<FieldDeclarationSyntax>();
            fields.Should().HaveCount(2);

            fields.First().Declarators.First().Initializer.Value.GetFirstToken().ValueText
                .Should().Be(datasetRelationshipName);

            fields.Skip(1).First().Declarators.First().Initializer.Value.GetFirstToken().ValueText
                .Should().Be(datasetRelationshipId);

            IEnumerable<PropertyStatementSyntax> properties = result.OfType<PropertyStatementSyntax>();
            properties.Should().HaveCount(6);

            string fl1Property = properties.Where(s => s.Identifier.ValueText == $"{CodeGenerationDatasetTypeConstants.FundingLinePrefix}_1_FundingLine1").First().ToFullString();
            fl1Property.Should().Contain($"Id := \"{CodeGenerationDatasetTypeConstants.FundingLinePrefix}_1\"");
            fl1Property.Should().Contain($"Public Property {CodeGenerationDatasetTypeConstants.FundingLinePrefix}_1_FundingLine1() As String");
            fl1Property.Should().Contain("IsAggregable := \"False\"");

            string fl2Property = properties.Where(s => s.Identifier.ValueText == $"{CodeGenerationDatasetTypeConstants.FundingLinePrefix}_3_FundingLine2").First().ToFullString();
            fl2Property.Should().Contain($"Id := \"{CodeGenerationDatasetTypeConstants.FundingLinePrefix}_3\"");
            fl2Property.Should().Contain("<ObsoleteItem()>");
            fl2Property.Should().Contain($"Public Property {CodeGenerationDatasetTypeConstants.FundingLinePrefix}_3_FundingLine2() As String");
            fl2Property.Should().Contain("IsAggregable := \"False\"");

            string fl3Property = properties.Where(s => s.Identifier.ValueText == $"{CodeGenerationDatasetTypeConstants.FundingLinePrefix}_4_FundingLine3").First().ToFullString();
            fl3Property.Should().Contain($"Id := \"{CodeGenerationDatasetTypeConstants.FundingLinePrefix}_4\"");
            fl3Property.Should().Contain("<ObsoleteItem()>");
            fl3Property.Should().Contain($"Public Property {CodeGenerationDatasetTypeConstants.FundingLinePrefix}_4_FundingLine3() As String");
            fl3Property.Should().Contain("IsAggregable := \"False\"");

            string calc2Property = properties.Where(s => s.Identifier.ValueText == $"{CodeGenerationDatasetTypeConstants.CalculationPrefix}_2_Calculation2").First().ToFullString();
            calc2Property.Should().Contain($"Id := \"{CodeGenerationDatasetTypeConstants.CalculationPrefix}_2\"");
            calc2Property.Should().Contain($"Public Property {CodeGenerationDatasetTypeConstants.CalculationPrefix}_2_Calculation2() As Integer");
            calc2Property.Should().Contain("IsAggregable := \"False\"");

            string ukprnProperty = properties.Where(s => s.Identifier.ValueText == $"{CodeGenerationDatasetTypeConstants.UKPRNFieldName}").First().ToFullString();
            ukprnProperty.Should().Contain($"Id := \"{CodeGenerationDatasetTypeConstants.UKPRNFieldName}\"");
            ukprnProperty.Should().Contain($"Name := \"{CodeGenerationDatasetTypeConstants.UKPRNFieldName}\"");
            ukprnProperty.Should().Contain("IsAggregable := \"False\"");

            string hasValueProperty = properties.Where(s => s.Identifier.ValueText == "HasValue").First().ToFullString();
            hasValueProperty.Should().Contain("Return whether the dataset exists for the current provider.");
            hasValueProperty.Should().Contain("Public Property HasValue As Boolean");
        }
    }
}
