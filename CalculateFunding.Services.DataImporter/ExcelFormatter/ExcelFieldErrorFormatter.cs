using System.Drawing;
using CalculateFunding.Services.DataImporter.Validators.Models;
using OfficeOpenXml;
using CalculateFunding.Services.DataImporter.Validators.Extension;
using OfficeOpenXml.Style;

namespace CalculateFunding.Services.DataImporter.ExcelFormatter
{
    public class ExcelFieldErrorFormatter : IExcelErrorFormatter
    {
	    private readonly ExcelPackage _excelPackage;

	    public ExcelFieldErrorFormatter(ExcelPackage excelPackage)
	    {
		    _excelPackage = excelPackage;
	    }

	    public void FormatExcelSheetBasedOnErrors(IDatasetUploadValidationResult validationResult)
	    {
		    ExcelWorksheet workbookWorksheet = _excelPackage.Workbook.Worksheets[1];

		    foreach (FieldValidationResult fieldValidationResult in validationResult.FieldValidationFailures)
		    {
			    DatasetUploadCellReference cellReferenceOfValidatedField = fieldValidationResult.FieldValidated.CellReference;
			    ExcelRange cell = workbookWorksheet.Cells[cellReferenceOfValidatedField.RowIndex, cellReferenceOfValidatedField.ColumnIndex];

			    Color colorCodeForFailure = fieldValidationResult.ReasonOfFailure.GetColorCodeForFailure();
			    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
			    cell.Style.Fill.BackgroundColor.SetColor(colorCodeForFailure);
		    }

			_excelPackage.Save();
	    }
    }
}
