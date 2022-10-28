using System;
using System.Collections.Generic;
using System.Drawing;
using CalculateFunding.Services.DataImporter.ExcelFormatter;
using CalculateFunding.Services.DataImporter.Validators.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;

namespace CalculateFunding.Services.Datasets.ExcelFormatter
{
    [TestClass]
    public class SheetTemplateCreatorTests
    {
        [TestMethod]
        public void CreateHeaderErrorSheet_WhenNotAlreadyExists_ShouldCreateWithExpectedFormatting()
        {
            using ExcelPackage excelPackage = new ExcelPackage();

            (string name, Color color)[] expectedColors = new  (string name, Color color)[]
            {
                ("Data type mismatch", Color.FromArgb(255, 138, 80)),
                ("Max. or Min. value exceeded", Color.FromArgb(255, 217, 102)),
                ("Provider ID value missing", Color.FromArgb(255, 255, 114)),
                ("Duplicate entries in the provider ID column", Color.FromArgb(122, 124, 255)),
                ("Provider ID not in scoped set of providers", Color.FromArgb(255, 178, 255)),
                ("New provider to be inserted. All data schema fields required on upload file for new providers.", Color.FromArgb(146, 208, 80)),
                ("Columns not included on the data schema", Color.FromArgb(255, 0, 0)),
                ("Provider ID not in the correct format", Color.FromArgb(0, 176, 240)),
                ("Duplicate column header", Color.FromArgb(50, 113, 168)),
            };

            SheetTemplate sheetTemplateCreated = ErrorSheetTemplateCreator.CreateHeaderErrorSheet(excelPackage);
            ExcelWorksheet templateWorkSheet = sheetTemplateCreated.ExcelWorksheet;
            DatasetUploadCellReference startingCellReference = sheetTemplateCreated.StartingCell;

            ExcelRange headerCell = templateWorkSheet.Cells[1, 1];
            headerCell.Value.Should().Be("Cell level error key");

            int firstColumn = 1;
            int errorColoringStartRow = 2;
            for (int row = errorColoringStartRow, index = 0; row < errorColoringStartRow + expectedColors.Length; row++, index++)
            {
                (string name, Color color) expectedColor = expectedColors[index];
                ExcelRange cell = templateWorkSheet.Cells[row, firstColumn];
                cell.Style.Fill.BackgroundColor.Rgb.Should().BeEquivalentTo(ToAsciRgbRepresentation(expectedColor.color));
            }

            int fieldsMissingTextRow = expectedColors.Length + errorColoringStartRow + 2;
            ExcelRange fieldsMissingCell = templateWorkSheet.Cells[fieldsMissingTextRow, firstColumn];
            fieldsMissingCell.Value.Should().Be(
                "Data schema fields missing from first sheet of Excel file to be uploaded");

            startingCellReference.ColumnIndex.Should().Be(firstColumn);
            startingCellReference.RowIndex.Should().Be(fieldsMissingTextRow + 1);
        }

        [TestMethod]
        public void CreateHeaderErrorSheet_WhenAlreadyExists_ShouldOverwriteSheet()
        {
            using ExcelPackage excelPackage = new ExcelPackage();
            // Arrange
            string valueToCheckWhetherChanged = "ChangedValue";

            SheetTemplate sheetTemplateCreated = ErrorSheetTemplateCreator.CreateHeaderErrorSheet(excelPackage);
            string worksheetName = sheetTemplateCreated.ExcelWorksheet.Name;
            ExcelWorksheet workSheetJustAdded = excelPackage.Workbook.Worksheets[worksheetName];
            workSheetJustAdded.Cells[1, 1].Value = valueToCheckWhetherChanged;
            workSheetJustAdded.Cells[1, 1].Value.Should().Be(valueToCheckWhetherChanged);

            // Act
            ErrorSheetTemplateCreator.CreateHeaderErrorSheet(excelPackage);
            ExcelWorksheet sheetOverwritten = excelPackage.Workbook.Worksheets[worksheetName];

            // Assert
            sheetOverwritten.Cells[1, 1].Value.Should().NotBe(valueToCheckWhetherChanged);
        }

        private string ToAsciRgbRepresentation(Color color)
        {
            return $"FF{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}