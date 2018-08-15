using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Services.DataImporter.Validators.Models;
using OfficeOpenXml;

namespace CalculateFunding.Services.DataImporter.ExcelFormatter
{
    public class SheetTemplate
    {
	    public SheetTemplate(ExcelWorksheet excelWorksheet, DatasetUploadCellReference startingCell)
	    {
		    ExcelWorksheet = excelWorksheet;
		    StartingCell = startingCell;
	    }

	    public ExcelWorksheet ExcelWorksheet { get; }

	    public DatasetUploadCellReference StartingCell{ get; }
    }
}
