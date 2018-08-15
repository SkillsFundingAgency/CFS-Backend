using System.Collections.Generic;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.ExcelFormatter
{
	public interface IExcelErrorFormatter
	{
		void FormatExcelSheetBasedOnErrors(IDatasetUploadValidationResult validationResult);
	}
}