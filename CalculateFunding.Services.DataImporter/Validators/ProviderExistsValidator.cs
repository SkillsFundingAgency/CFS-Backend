using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.DataImporter.Validators.Extension;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.Validators
{
    public class ProviderExistsValidator : IFieldValidator
    {
	    private readonly IList<ProviderSummary> _providerSummaries;

	    public ProviderExistsValidator(IList<ProviderSummary> providerSummaries)
	    {
		    _providerSummaries = providerSummaries;
	    }

	    private bool PreValidation(Field field)
	    {
		    return field.FieldDefinition.IdentifierFieldType.HasValue;
	    }

	    public FieldValidationResult ValidateField(Field field)
	    {
			if (PreValidation(field))
			{
				ProviderSummary providerSummary = _providerSummaries.FirstOrDefault(p =>
					p.GetIdentifierBasedOnIdentifierType(field.FieldDefinition.IdentifierFieldType) == field.Value.ToString());
				
				if (providerSummary == null)
				{
					return new FieldValidationResult(field, DatasetCellReasonForFailure.ProviderIdMismatchWithServiceProvider);
				}
			}
		    return null;
	    }
	}
}
