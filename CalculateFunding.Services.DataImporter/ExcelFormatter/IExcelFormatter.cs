using System.Collections.Generic;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.ExcelFormatter
{
	public interface IExcelFieldFormatter
	{
		void FormatExcelSheetBasedOnErrors(IDatasetUploadValidationResult validationResult);
	}
}