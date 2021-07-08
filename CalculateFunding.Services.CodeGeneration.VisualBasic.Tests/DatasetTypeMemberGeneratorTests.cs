using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
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
            IEnumerable<StatementSyntax> result = sut.GetMembers(datasetRelationshipSummary);

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
            DatasetRelationshipSummary datasetRelationshipSummary = new DatasetRelationshipSummaryBuilder()
                .WithType(DatasetRelationshipType.ReleasedData)
                .WithPublishedSpecificationConfigurationFundingLines(new List<PublishedSpecificationItem>
                {
                    new PublishedSpecificationItem
                    {
                        TemplateId = 1,
                        Name = "Funding Line 1",
                        SourceCodeName = "FL1",
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
            IEnumerable<StatementSyntax> result = sut.GetMembers(datasetRelationshipSummary);

            IEnumerable<PropertyStatementSyntax> properties = result.OfType<PropertyStatementSyntax>();
            properties.Should().HaveCount(4);

            string fl1Property = properties.Where(s => s.Identifier.ValueText == $"{CodeGenerationDatasetTypeConstants.FundingLinePrefix}_1_FundingLine1").First().ToFullString();
            fl1Property.Should().Contain($"Id := \"{CodeGenerationDatasetTypeConstants.FundingLinePrefix}_1\"");
            fl1Property.Should().Contain($"Public Property {CodeGenerationDatasetTypeConstants.FundingLinePrefix}_1_FundingLine1() As String");
            fl1Property.Should().Contain("IsAggregable := \"False\"");

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
