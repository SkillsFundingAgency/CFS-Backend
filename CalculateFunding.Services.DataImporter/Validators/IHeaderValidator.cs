using System.Collections.Generic;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.Validators
{
    public interface IHeaderValidator
    {
	    IList<HeaderValidationResult> ValidateHeaders(IList<string> headerFields);
    }
}
