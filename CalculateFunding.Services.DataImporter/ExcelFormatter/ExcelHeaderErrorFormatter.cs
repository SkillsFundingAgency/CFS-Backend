using System.Drawing;
using System.Linq;
using CalculateFunding.Services.DataImporter.Validators.Extension;
using CalculateFunding.Services.DataImporter.Validators.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace CalculateFunding.Services.DataImporter.ExcelFormatter
{
    public class ExcelHeaderErrorFormatter : IExcelErrorFormatter
    {
	    private readonly ExcelPackage _excelPackage;

	    public ExcelHeaderErrorFormatter(ExcelPackage excelPackage)
	    {
		    _excelPackage = excelPackage;
	    }

	    public void FormatExcelSheetBasedOnErrors(IDatasetUploadValidationResult validationResult)
	    {
		    if (!validationResult.IsValid)
		    {
			    InsertHeaderErrorsWithoutKeyColour(validationResult);
			    FormatHeaderErrorsWithKeyColour(validationResult);
		    }
	    }

	    private void FormatHeaderErrorsWithKeyColour(IDatasetUploadValidationResult validationResult)
	    {
		    ExcelWorksheet workbookWorksheet = _excelPackage.Workbook.Worksheets[1];

		    foreach (HeaderValidationResult headerValidationResult in validationResult.HeaderValidationFailures.Where(_ => _.HasBackgroundKeyColour))
		    {
			    Color colorCodeForFailure = headerValidationResult.ReasonForFailure.GetColorCodeForFailure();

			    SetErrorStyle(headerValidationResult, workbookWorksheet, colorCodeForFailure);
		    }
	    }

	    private void SetErrorStyle(HeaderValidationResult headerValidationResult,
		    ExcelWorksheet worksheet,
		    Color colour)
	    {
		    for (int column = 1; column <= worksheet.Dimension.Columns ; column++)
		    {
			    ExcelRange cell = worksheet.Cells[1, column];

			    string headerName = headerValidationResult.FieldDefinition.Name.ToLowerInvariant().Trim();
			    
			    if (((string) cell.Value)?.ToLowerInvariant().Trim() == headerName)
			    {
				    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
				    cell.Style.Fill.BackgroundColor.SetColor(colour);
			    }
		    }
	    }

	    private void InsertHeaderErrorsWithoutKeyColour(IDatasetUploadValidationResult validationResult)
	    {
		    SheetTemplate templateReturned = ErrorSheetTemplateCreator.CreateHeaderErrorSheet(_excelPackage);
		    HeaderValidationResult[] headerValidationFailures = validationResult.HeaderValidationFailures.Where(_ => !_.HasBackgroundKeyColour).ToArray();
		    DatasetUploadCellReference errorInsertionsStartingCell = templateReturned.StartingCell;

		    ExcelWorksheet headerErrorsWorksheet = _excelPackage.Workbook.Worksheets[templateReturned.ExcelWorksheet.Name];

		    for (int index = 0, rowIndex = errorInsertionsStartingCell.RowIndex; index < headerValidationFailures.Length; rowIndex++, index++)
		    {
			    ExcelRange cellToInsertErrorInto = headerErrorsWorksheet.Cells[rowIndex, 1];

			    cellToInsertErrorInto.Value = headerValidationFailures[index].FieldDefinition.Name;
		    }
	    }
    }
}
