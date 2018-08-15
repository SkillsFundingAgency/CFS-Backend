using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.DataImporter.Validators.Models
{
    public class DatasetUploadValidationResult : IDatasetUploadValidationResult
    {
		public IEnumerable<FieldValidationResult> FieldValidationFailures { get; set; } = new List<FieldValidationResult>();
	    public IEnumerable<HeaderValidationResult> HeaderValitionFailures { get; set; } = new List<HeaderValidationResult>();

	    public bool IsValid()
	    {
		    return FieldValidationFailures.IsNullOrEmpty() && HeaderValitionFailures.IsNullOrEmpty();
	    }
    }
}
