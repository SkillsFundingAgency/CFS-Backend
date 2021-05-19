using CalculateFunding.Models.Datasets.Schema;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CalculateFunding.Services.DataImporter.UnitTests
{
    [TestClass]
    public class DataDefinitionExcelWriterTests
    {
        [TestMethod]
        public void Write_GivenNullTableDefinitions_ReturnsNull()
        {
            //Arrange
            DatasetDefinition datasetDefinition = new DatasetDefinition();

            DataDefinitionExcelWriter writer = new DataDefinitionExcelWriter();

            //Act
            byte[] excelBytes = writer.Write(datasetDefinition);

            //Assert
            excelBytes
                .Should()
                .BeNull();
        }

        [TestMethod]
        public void Write_GivenEmptyTableDefinitions_ReturnsNull()
        {
            //Arrange
            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                TableDefinitions = new List<TableDefinition>()
            };

            DataDefinitionExcelWriter writer = new DataDefinitionExcelWriter();

            //Act
            byte[] excelBytes = writer.Write(datasetDefinition);

            //Assert
            excelBytes
                .Should()
                .BeNull();
        }

        [TestMethod]
        public void Write_GivenValidDefinitionWithOneTableDefinition_CreatesExcelBytes()
        {
            //Arrange
            DatasetDefinition datasetDefinition = CreateDatasetDefinitionWithOneTableDefinition();

            DataDefinitionExcelWriter writer = new DataDefinitionExcelWriter();

            //Act
            byte[] excelBytes = writer.Write(datasetDefinition);

            //Assert
            excelBytes
                .Should()
                .NotBeNull();

            excelBytes
                .Length
                .Should()
                .BeGreaterThan(0);

            using (Stream excelStream = new MemoryStream(excelBytes))
            {
                ExcelPackage excelPackage = new ExcelPackage(excelStream);

                excelPackage.Workbook.Should().NotBeNull();
                excelPackage.Workbook.Worksheets.Count.Should().Be(1);
                excelPackage.Workbook.Worksheets.First().Name.Should().Be("Test Table Def 1");
                excelPackage.Workbook.Worksheets.First().Cells.Count().Should().Be(2);
                excelPackage.Workbook.Worksheets.First().Cells[1, 1].Value.Should().Be("Test field name 1");
                excelPackage.Workbook.Worksheets.First().Cells[1, 1].Comment.Should().NotBeNull();
                excelPackage.Workbook.Worksheets.First().Cells[1, 1].Comment.Text.Should().Be("Description: Test description 1\n Required: No\n Type: String");
                excelPackage.Workbook.Worksheets.First().Cells[1, 2].Value.Should().Be("Test field name 2");
                excelPackage.Workbook.Worksheets.First().Cells[1, 2].Comment.Should().NotBeNull();
                excelPackage.Workbook.Worksheets.First().Cells[1, 2].Comment.Text.Should().Be("Description: Test description 2\n Required: No\n Type: String");
            }
        }

        [TestMethod]
        public void Write_GivenValidDefinitionWithTwoTableDefinitions_CreatesExcelBytes()
        {
            //Arrange
            DatasetDefinition datasetDefinition = CreateDatasetDefinitionWithTwoTableDefinitions();

            DataDefinitionExcelWriter writer = new DataDefinitionExcelWriter();

            //Act
            byte[] excelBytes = writer.Write(datasetDefinition);

            //Assert
            excelBytes
                .Should()
                .NotBeNull();

            excelBytes
                .Length
                .Should()
                .BeGreaterThan(0);

            using (Stream excelStream = new MemoryStream(excelBytes))
            {
                ExcelPackage excelPackage = new ExcelPackage(excelStream);

                excelPackage.Workbook.Should().NotBeNull();
                excelPackage.Workbook.Worksheets.Count.Should().Be(2);
                excelPackage.Workbook.Worksheets.First().Name.Should().Be("Test Table Def 1");
                excelPackage.Workbook.Worksheets.First().Cells.Count().Should().Be(2);
                excelPackage.Workbook.Worksheets.First().Cells[1, 1].Value.Should().Be("Test field name 1");
                excelPackage.Workbook.Worksheets.First().Cells[1, 1].Comment.Should().NotBeNull();
                excelPackage.Workbook.Worksheets.First().Cells[1, 1].Comment.Text.Should().Be("Description: Test description 1\n Required: No\n Type: String");
                excelPackage.Workbook.Worksheets.First().Cells[1, 2].Value.Should().Be("Test field name 2");
                excelPackage.Workbook.Worksheets.First().Cells[1, 2].Comment.Should().NotBeNull();
                excelPackage.Workbook.Worksheets.First().Cells[1, 2].Comment.Text.Should().Be("Description: Test description 2\n Required: No\n Type: String");
                excelPackage.Workbook.Worksheets.Last().Name.Should().Be("Test Table Def 2");
                excelPackage.Workbook.Worksheets.Last().Cells.Count().Should().Be(2);
                excelPackage.Workbook.Worksheets.Last().Cells[1, 1].Value.Should().Be("Test field name 3");
                excelPackage.Workbook.Worksheets.Last().Cells[1, 1].Comment.Should().NotBeNull();
                excelPackage.Workbook.Worksheets.Last().Cells[1, 1].Comment.Text.Should().Be("Description: Test description 3\n Required: No\n Type: String");
                excelPackage.Workbook.Worksheets.Last().Cells[1, 2].Value.Should().Be("Test field name 4");
                excelPackage.Workbook.Worksheets.Last().Cells[1, 2].Comment.Should().NotBeNull();
                excelPackage.Workbook.Worksheets.Last().Cells[1, 2].Comment.Text.Should().Be("Description: Test description 4\n Required: No\n Type: String");
            }
        }

        static DatasetDefinition CreateDatasetDefinitionWithOneTableDefinition()
        {
            return new DatasetDefinition
            {
                Id = "12345",
                Name = "14/15",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition
                    {
                        Id = "1111",
                        Name = "Test Table Def 1",
                        FieldDefinitions = new List<FieldDefinition>
                        {
                            new FieldDefinition
                            {
                                Id = "FD111",
                                Name = "Test field name 1",
                                Description = "Test description 1",
                                Type = FieldType.String,
                                IdentifierFieldType = IdentifierFieldType.LACode
                            },
                            new FieldDefinition
                            {
                                Id = "FD222",
                                Name = "Test field name 2",
                                Description = "Test description 2",
                                Type = FieldType.String,
                                IdentifierFieldType = null
                            },
                        }
                    }
                }
            };
        }

        static DatasetDefinition CreateDatasetDefinitionWithTwoTableDefinitions()
        {
            return new DatasetDefinition
            {
                Id = "12345",
                Name = "14/15",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition
                    {
                        Id = "1111",
                        Name = "Test Table Def 1",
                        FieldDefinitions = new List<FieldDefinition>
                        {
                            new FieldDefinition
                            {
                                Id = "FD111",
                                Name = "Test field name 1",
                                Description = "Test description 1",
                                Type = FieldType.String,
                                IdentifierFieldType = IdentifierFieldType.LACode
                            },
                            new FieldDefinition
                            {
                                Id = "FD222",
                                Name = "Test field name 2",
                                Description = "Test description 2",
                                Type = FieldType.String,
                                IdentifierFieldType = null
                            },
                        }
                    },
                    new TableDefinition
                    {
                        Id = "2222",
                        Name = "Test Table Def 2",
                        FieldDefinitions = new List<FieldDefinition>
                        {
                            new FieldDefinition
                            {
                                Id = "FD333",
                                Name = "Test field name 3",
                                Description = "Test description 3",
                                Type = FieldType.String,
                                IdentifierFieldType = IdentifierFieldType.LACode
                            },
                            new FieldDefinition
                            {
                                Id = "FD444",
                                Name = "Test field name 4",
                                Description = "Test description 4",
                                Type = FieldType.String,
                                IdentifierFieldType = null
                            },
                        }
                    }
                }
            };
        }
    }
}
