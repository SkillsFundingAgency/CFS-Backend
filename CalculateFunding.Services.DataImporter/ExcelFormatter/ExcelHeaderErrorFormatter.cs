using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Services.DataImporter.Validators.Models;
using OfficeOpenXml;

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
		    SheetTemplate templateReturned = ErrorSheetTemplateCreator.CreateHeaderErrorSheet(_excelPackage);
		    List<HeaderValidationResult> headerValidationFailures = validationResult.HeaderValitionFailures.ToList();
		    DatasetUploadCellReference errorInsertionsStartingCell = templateReturned.StartingCell;

			ExcelWorksheet headerErrorsWorksheet = _excelPackage.Workbook.Worksheets[templateReturned.ExcelWorksheet.Name];


		    for (int index = 0, rowIndex = errorInsertionsStartingCell.RowIndex; index < headerValidationFailures.Count; rowIndex++, index++)
		    {
			    ExcelRange cellToInsertErrorInto = headerErrorsWorksheet.Cells[rowIndex, 1];

			    cellToInsertErrorInto.Value = headerValidationFailures[index].FieldDefinitionValidated.Name;
		    }
	    }
    }
}
