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
			using (var excelPackage = new ExcelPackage())
			{
				// Arrange
				List<Tuple<string, Color>> expectedColors = new List<Tuple<string, Color>>
				{
					new Tuple<string, Color>("Data type mismatch", Color.FromArgb(255, 138, 80)),
					new Tuple<string, Color>("Max. or Min. value exceeded", Color.FromArgb(255, 217, 102)),
					new Tuple<string, Color>("Provider ID value missing", Color.FromArgb(255, 255, 114)),
					new Tuple<string, Color>("Duplicate entries in the provider ID column", Color.FromArgb(122, 124, 255)),
					new Tuple<string, Color>("Provider ID mismatch with service provider data", Color.FromArgb(255, 178, 255))
				};

				// Act
				SheetTemplate sheetTemplateCreated = ErrorSheetTemplateCreator.CreateHeaderErrorSheet(excelPackage);
				ExcelWorksheet templateWorkSheet = sheetTemplateCreated.ExcelWorksheet;
				DatasetUploadCellReference startingCellReference = sheetTemplateCreated.StartingCell;


				// Assert
				ExcelRange headerCell = templateWorkSheet.Cells[1, 1];
				headerCell.Value.ShouldBeEquivalentTo("Cell level error key");

				int firstColumn = 1;
				int errorColoringStartRow = 2;
				for (int row = errorColoringStartRow, index = 0; row < errorColoringStartRow + expectedColors.Count; row++, index++)
				{
					Tuple<string, Color> expectedColor = expectedColors[index];
					ExcelRange cell = templateWorkSheet.Cells[row, firstColumn];
					cell.Style.Fill.BackgroundColor.Rgb.ShouldBeEquivalentTo(ToAsciRgbRepresentation(expectedColor.Item2));
				}

				int fieldsMissingTextRow = expectedColors.Count + errorColoringStartRow + 2;
				ExcelRange fieldsMissingCell = templateWorkSheet.Cells[fieldsMissingTextRow, firstColumn];
				fieldsMissingCell.Value.ShouldBeEquivalentTo(
					"Data schema fields missing from first sheet of Excel file to be uploaded");


				startingCellReference.ColumnIndex.ShouldBeEquivalentTo(firstColumn);
				startingCellReference.RowIndex.ShouldBeEquivalentTo(fieldsMissingTextRow + 1);
			}
		}

		[TestMethod]
		public void CreateHeaderErrorSheet_WhenAlreadyExists_ShouldOverwriteSheet()
		{
			using (var excelPackage = new ExcelPackage())
			{
				// Arrange
				string valueToCheckWhetherChanged = "ChangedValue";

				SheetTemplate sheetTemplateCreated = ErrorSheetTemplateCreator.CreateHeaderErrorSheet(excelPackage);
				string worksheetName = sheetTemplateCreated.ExcelWorksheet.Name;
				ExcelWorksheet workSheetJustAdded = excelPackage.Workbook.Worksheets[worksheetName];
				workSheetJustAdded.Cells[1, 1].Value = valueToCheckWhetherChanged;
				workSheetJustAdded.Cells[1, 1].Value.ShouldBeEquivalentTo(valueToCheckWhetherChanged);

				// Act
				ErrorSheetTemplateCreator.CreateHeaderErrorSheet(excelPackage);
				ExcelWorksheet sheetOverwritten = excelPackage.Workbook.Worksheets[worksheetName];

				// Assert
				sheetOverwritten.Cells[1, 1].Value.Should().NotBe(valueToCheckWhetherChanged);
			}
		}

		private string ToAsciRgbRepresentation(Color color)
		{
			return $"FF{color.R:X}{color.G:X}{color.B:X}";
		}
	}
}