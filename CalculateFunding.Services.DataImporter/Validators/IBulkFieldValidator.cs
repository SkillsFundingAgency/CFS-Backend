using System.Collections.Generic;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.Validators
{
	public interface IBulkFieldValidator
	{
		IList<FieldValidationResult> ValidateAllFields(IList<Field> allFields);
	}
}