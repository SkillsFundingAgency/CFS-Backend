using System.Collections.Generic;

namespace CalculateFunding.Services.DataImporter.Validators.Models
{
    public interface IDatasetUploadValidationResult
    {
	    IEnumerable<FieldValidationResult> FieldValidationFailures { get; }

		IEnumerable<HeaderValidationResult> HeaderValidationFailures { get; }

		bool IsValid { get; }
    }
}
