using CalculateFunding.Services.Datasets.Excel;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace CalculateFunding.Services.Publishing.UnitTests.Excel
{
    [TestClass]
    public class RelationshipDataExcelWriterTests
    {
        [TestMethod]
        public void WriteToExcel_GivenNoData_ThrowsException()
        {
            // Arrange
            IRelationshipDataExcelWriter writer = CreateWriter();
            string workSheetName = NewRandomString();

            // Act
            Func<byte[]> result = () => writer.WriteToExcel(workSheetName, Enumerable.Empty<RelationshipDataSetExcelData>());

            // Assert
            result
                .Should()
                .Throw<Exception>()
                .WithMessage("RelationshipDataSet is empty to create excel.");
        }

        [TestMethod]
        public async Task WriteToExcel_GivenValidData_CreatesExcel()
        {
            // Arrange
            IRelationshipDataExcelWriter writer = CreateWriter();
            string worksheetName = NewRandomString().Substring(0,30);
            string Ukprn1 = NewRandomString();
            string Ukprn2 = NewRandomString();
            string Ukprn3 = NewRandomString();
            string fundingLine1 = $"FL_{NewRandomString()}";
            string fundingLine2 = $"FL_{NewRandomString()}";
            decimal fundingLineValue1 = NewRandomDecimal();
            decimal fundingLineValue2 = NewRandomDecimal();
            string calculation1 = $"Calc_{NewRandomString()}";
            string calculation2 = $"Calc_{NewRandomString()}";
            decimal calculationValue1 = NewRandomDecimal();
            decimal calculationValue2 = NewRandomDecimal();

            IEnumerable<RelationshipDataSetExcelData> datasetData = new[]
            {
                new RelationshipDataSetExcelData(Ukprn1) 
                {
                    FundingLines = new Dictionary<string, decimal?> { {fundingLine1, fundingLineValue1 }, { fundingLine2, fundingLineValue2 } },
                    Calculations = new Dictionary<string, decimal?>{ {calculation1, null }, {calculation2, calculationValue2 } }
                },
                new RelationshipDataSetExcelData(Ukprn2)
                {
                    FundingLines = new Dictionary<string, decimal?> { { fundingLine1, null }, { fundingLine2, fundingLineValue2 } },
                    Calculations = new Dictionary<string, decimal?>{ {calculation1, calculationValue1 }, {calculation2, null } }
                },
                new RelationshipDataSetExcelData(Ukprn3)
                {
                    FundingLines = new Dictionary<string, decimal?> { {fundingLine1, fundingLineValue1 }, { fundingLine2, null } },
                    Calculations = new Dictionary<string, decimal?>{ { calculation1, calculationValue1 }, { calculation2, calculationValue2 } }
                }
            };

            // Act
            byte[] excelDataBytes = writer.WriteToExcel(worksheetName, datasetData);

            // Assert
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName())) + ".xlsx";
            await File.WriteAllBytesAsync(tempFile, excelDataBytes);

            IEnumerable<RelationshipDataSetExcelData> excelData = ReadFromExcel(tempFile, worksheetName);

            excelData
                .Should()
                .HaveCount(3);
            excelData
                .Should()
                .BeEquivalentTo(datasetData);
        }

        private IRelationshipDataExcelWriter CreateWriter()
        {
            return new RelationshipDataExcelWriter(new Mock<ILogger>().Object);
        }

        private string NewRandomString() => new RandomString();
        private decimal NewRandomDecimal() => decimal.Parse($"{new RandomNumberBetween(0, int.MaxValue)}.{new RandomNumberBetween(0, 99)}");

        private IEnumerable<RelationshipDataSetExcelData> ReadFromExcel(string fileName, string worksheetName)
        {
            List<RelationshipDataSetExcelData> excelData = new List<RelationshipDataSetExcelData>();

            using ExcelPackage package = new ExcelPackage(new FileInfo(fileName));
            ExcelWorksheet worksheet = package.Workbook.Worksheets[worksheetName];

            int columnCount = worksheet.Dimension.Columns;
            int rowCount = worksheet.Dimension.Rows;
            ExcelRange cells = worksheet.Cells;

            IList<string> headers = new List<string>();

            for (int i = 1; i <= columnCount; i++)
            {
                headers.Add(cells[1, i].Value.ToString());
            }

            for (int i = 2; i <= rowCount; i++)
            {
                RelationshipDataSetExcelData dataItem = new RelationshipDataSetExcelData(cells[i,1].Value.ToString());
                for (int j = 2; j <= columnCount; j++)
                {
                    string columnHeader = headers[j-1];
                    string rowValue = cells[i, j].Value?.ToString();

                    KeyValuePair<string, decimal?> keyValue = new KeyValuePair<string, decimal?>(columnHeader, string.IsNullOrEmpty(rowValue) ? (decimal?)null : decimal.Parse(rowValue));

                    if (columnHeader.StartsWith("FL"))
                    {
                        dataItem.FundingLines.Add(keyValue);
                    }
                    else
                    {
                        dataItem.Calculations.Add(keyValue);
                    }
                }

                excelData.Add(dataItem);
            }

            return excelData;
        }
    }
}
