using System.Collections.Generic;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.Validators
{
    public interface IHeaderValidator
    {
	    IEnumerable<HeaderValidationResult> ValidateHeaders(IEnumerable<string> headerFields);
    }
}
