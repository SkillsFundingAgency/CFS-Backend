using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.DataImporter.Validators.Models
{
    public interface IDatasetUploadValidationResult
    {
	    IEnumerable<FieldValidationResult> FieldValidationFailures { get; }

		IEnumerable<HeaderValidationResult> HeaderValitionFailures { get; }
	}
}
