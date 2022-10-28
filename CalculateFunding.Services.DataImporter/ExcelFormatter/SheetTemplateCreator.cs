using System.Collections.Generic;
using System.Drawing;
using CalculateFunding.Services.DataImporter.Validators.Extension;
using CalculateFunding.Services.DataImporter.Validators.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace CalculateFunding.Services.DataImporter.ExcelFormatter
{
    public static class ErrorSheetTemplateCreator
    {
	    public static SheetTemplate CreateHeaderErrorSheet(ExcelPackage excelPackage)
	    {
		    List<CellLevelErrorKey> cellLevelErrorKeys = new List<CellLevelErrorKey>()
		    {
			    new CellLevelErrorKey(DatasetCellReasonForFailure.DataTypeMismatch, "Data type mismatch"),
			    new CellLevelErrorKey(DatasetCellReasonForFailure.MaxOrMinValueExceeded, "Max. or Min. value exceeded"),
			    new CellLevelErrorKey(DatasetCellReasonForFailure.ProviderIdValueMissing, "Provider ID value missing"),
			    new CellLevelErrorKey(DatasetCellReasonForFailure.DuplicateEntriesInTheProviderIdColumn, "Duplicate entries in the provider ID column"),
			    new CellLevelErrorKey(DatasetCellReasonForFailure.ProviderIdMismatchWithServiceProvider, "Provider ID not in scoped set of providers"),
				new CellLevelErrorKey(DatasetCellReasonForFailure.NewProviderMissingAllDataSchemaFields, "New provider to be inserted. All data schema fields required on upload file for new providers."),
				new CellLevelErrorKey(DatasetCellReasonForFailure.ExtraHeaderField, "Extra header fields that does not exists in the current data schema"),
				new CellLevelErrorKey(DatasetCellReasonForFailure.ProviderIdNotInCorrectFormat, "Provider ID not in the correct format"),
				new CellLevelErrorKey(DatasetCellReasonForFailure.DuplicateColumnHeader, "Duplicate column headers"),
			};
			
			if(excelPackage.Workbook.Worksheets["Errors"] != null) excelPackage.Workbook.Worksheets.Delete("Errors");
			ExcelWorksheet workSheetAdded = excelPackage.Workbook.Worksheets.Add("Errors");
			workSheetAdded.Cells[1, 1].Value = "Cell level error key";

			const int firstColumn = 1;
			const int secondColumn = 2;

			int row = 2;
			for (int index = 0; index < cellLevelErrorKeys.Count; row++, index++)
			{
				CellLevelErrorKey errorColorCoding = cellLevelErrorKeys[index];

				ExcelRange cellToColorIn = workSheetAdded.Cells[row, firstColumn];
				ExcelRange cellForErrorMessage = workSheetAdded.Cells[row, secondColumn];

				cellToColorIn.Style.Fill.PatternType = ExcelFillStyle.Solid;
				cellToColorIn.Style.Fill.BackgroundColor.SetColor(errorColorCoding.Color);

				cellForErrorMessage.Value = errorColorCoding.Errormessage;
			}

			int dataSchemaFieldsMissingFieldRow = row + 2;
			workSheetAdded.Cells[dataSchemaFieldsMissingFieldRow, firstColumn].Value = "Data schema fields missing from first sheet of Excel file to be uploaded";
			return new SheetTemplate(workSheetAdded, new DatasetUploadCellReference(dataSchemaFieldsMissingFieldRow + 1, firstColumn));
	    }

		private class CellLevelErrorKey
		{
			public CellLevelErrorKey(DatasetCellReasonForFailure error, string errorMessage)
			{
				Error = error;
				Color = Error.GetColorCodeForFailure();
				Errormessage = errorMessage;
			}

			private DatasetCellReasonForFailure Error { get; }

			public string Errormessage { get; }

			public Color Color { get; }
		}
    }
}
