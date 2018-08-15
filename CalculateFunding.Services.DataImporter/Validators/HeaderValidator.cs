using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.Validators
{
    public class RequiredHeaderExistsValidator : IHeaderValidator
    {
	    private readonly IList<FieldDefinition> _fieldDefinitions;

		public RequiredHeaderExistsValidator(IList<FieldDefinition> fieldDefinitions)
	    {
		    _fieldDefinitions = fieldDefinitions;
	    }
		
	    public IList<HeaderValidationResult> ValidateHeaders(IList<string> headerFields)
	    {
		    return
			    _fieldDefinitions
				    .Where(f => headerFields.All(h => h != f.Name))
					.Select(f => new HeaderValidationResult(f))
				    .ToList();
	    }
    }
}
