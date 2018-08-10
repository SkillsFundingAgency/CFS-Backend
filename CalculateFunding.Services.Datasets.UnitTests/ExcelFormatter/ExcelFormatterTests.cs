using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.ExcelFormatter;
using CalculateFunding.Services.DataImporter.Validators.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace CalculateFunding.Services.Datasets.ExcelFormatter
{
	[TestClass]
    public class ExcelFormatterTests
    {
		private const string testXlsxLocation = @"TestItems/TestCheck.xlsx";

		[TestMethod]
	    public void FormatExcelSheetBasedOnErrors_WhenValidationErrorsExists_ShouldColorWorksheet()
	    {
		    // Arrange;
		    FieldDefinition anyFieldDefinition = new FieldDefinition();
		    HashSet<Tuple<int, int>> fieldsInvalidated = new HashSet<Tuple<int, int>>()
		    {
			    new Tuple<int, int>(1, 1),
			    new Tuple<int, int>(2, 20),
			    new Tuple<int, int>(3, 5),
			    new Tuple<int, int>(4, 9),
		    };
		    string anyString = string.Empty;

		    List<FieldValidationResult> fieldValidationResults =
			    fieldsInvalidated
				    .Select(t => new FieldValidationResult(CreateField(t.Item1, t.Item2, anyString, anyFieldDefinition), false,
					    FieldValidationResult.ReasonForFailure.ProviderIdMismatchWithServiceProvider))
				    .ToList();

		    DatasetUploadValidationResult datasetUploadValidationResult = new DatasetUploadValidationResult()
		    {
			    FieldValidationFailures = fieldValidationResults
		    };

			// Act
			File.Copy(@"TestItems/1718HNStudNumbers.xlsx", testXlsxLocation, true);
		    FileInfo testFile = new FileInfo(testXlsxLocation);

			using (var excelPackage = new ExcelPackage(testFile))
			{
				ExcelFieldFormatter excelFieldFormatter = new ExcelFieldFormatter(excelPackage);
				excelFieldFormatter.FormatExcelSheetBasedOnErrors(datasetUploadValidationResult);
			}

		    //Assert
		    using (var excelPackage = new ExcelPackage(testFile))
		    {
			    CheckThatAllExpectedCellsAreColored(excelPackage, fieldsInvalidated);
		    }
	    }

	    [TestMethod]
	    public void FormatExcelSheetBasedOnErrors_WhenGivenEmptyValidation_ShouldNotDoAnything()
	    {
		    // Arrange;
		    DatasetUploadValidationResult datasetUploadValidationResult = new DatasetUploadValidationResult()
		    {
			    FieldValidationFailures = new List<FieldValidationResult>()
		    };

		    // Act
		    File.Copy(@"TestItems/1718HNStudNumbers.xlsx", testXlsxLocation, true);
		    FileInfo testFile = new FileInfo(testXlsxLocation);

		    using (var excelPackage = new ExcelPackage(testFile))
		    {
			    ExcelFieldFormatter excelFieldFormatter = new ExcelFieldFormatter(excelPackage);
			    excelFieldFormatter.FormatExcelSheetBasedOnErrors(datasetUploadValidationResult);
		    }

		    //Assert
		    using (var excelPackage = new ExcelPackage(testFile))
		    {
			    CheckThatAllExpectedCellsAreColored(excelPackage, new HashSet<Tuple<int, int>>());
		    }
	    }

		private static void CheckThatAllExpectedCellsAreColored(ExcelPackage excelPackage, HashSet<Tuple<int, int>> fieldsInvalidated)
	    {
		    ExcelWorksheet workbookWorksheet = excelPackage.Workbook.Worksheets[1];
		    int columnEnd = workbookWorksheet.Dimension.Columns;
		    int rowEnd = workbookWorksheet.Dimension.Rows;

		    for (int i = 1; i < rowEnd; i++)
		    {
			    for (int j = 1; j < columnEnd; j++)
			    {
				    ExcelColor cellColor = workbookWorksheet.Cells[i, j].Style.Fill.BackgroundColor;
				    if (fieldsInvalidated.Contains(new Tuple<int, int>(i, j)))
				    {
					    cellColor.Rgb
						    .Should().NotBeNull();
				    }
				    else
				    {
					    cellColor.Rgb
						    .Should().BeNull();
				    }
			    }
		    }
	    }

	    private static Field CreateField(int rowIndex, int columnIndex, object value, FieldDefinition fieldDefinition)
	    {
		    return new Field(new DatasetUploadCellReference(rowIndex, columnIndex), value, fieldDefinition);
	    }
	}
}
