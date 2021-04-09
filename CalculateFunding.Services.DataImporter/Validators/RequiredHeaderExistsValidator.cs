using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.Validators
{
	public class RequiredHeaderExistsValidator : IHeaderValidator
    {
	    private readonly FieldDefinition[] _fieldDefinitions;

		public RequiredHeaderExistsValidator(IEnumerable<FieldDefinition> fieldDefinitions)
	    {
		    _fieldDefinitions = fieldDefinitions.ToArray();
	    }
		
	    public IEnumerable<HeaderValidationResult> ValidateHeaders(IEnumerable<string> headerFields)
	    {
		    return
			    _fieldDefinitions
				    .Where(f => headerFields.All(h => h != f.Name))
					.Select(f => new HeaderValidationResult(f));
	    }
    }
}
