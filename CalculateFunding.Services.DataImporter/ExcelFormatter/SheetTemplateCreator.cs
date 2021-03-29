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
			    new CellLevelErrorKey(FieldValidationResult.ReasonForFailure.DataTypeMismatch, "Data type mismatch"),
			    new CellLevelErrorKey(FieldValidationResult.ReasonForFailure.MaxOrMinValueExceeded, "Max. or Min. value exceeded"),
			    new CellLevelErrorKey(FieldValidationResult.ReasonForFailure.ProviderIdValueMissing, "Provider ID value missing"),
			    new CellLevelErrorKey(FieldValidationResult.ReasonForFailure.DuplicateEntriesInTheProviderIdColumn, "Duplicate entries in the provider ID column"),
			    new CellLevelErrorKey(FieldValidationResult.ReasonForFailure.ProviderIdMismatchWithServiceProvider, "Provider ID does not exist in the current funding stream provider"),
				new CellLevelErrorKey(FieldValidationResult.ReasonForFailure.NewProviderMissingAllDataSchemaFields, "New provider to be inserted. All data schema fields required on upload file for new providers."),
				new CellLevelErrorKey(FieldValidationResult.ReasonForFailure.ExtraHeaderField, "Extra header fields that does not exists in the current data schema"),
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
			public CellLevelErrorKey(FieldValidationResult.ReasonForFailure error, string errorMessage)
			{
				Error = error;
				Color = Error.GetColorCodeForFailure();
				Errormessage = errorMessage;
			}

			public FieldValidationResult.ReasonForFailure Error { get; }

			public string Errormessage { get; }

			public Color Color { get; }
		}
    }
}
