using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.Validators
{
    public class ProviderIdentifierBlankValidator : IFieldValidator
    {
	    private bool PreValidate(Field field)
	    {
		    return field.FieldDefinition.IdentifierFieldType.HasValue;
	    }

	    public FieldValidationResult ValidateField(Field field)
	    {
		    if (PreValidate(field) && string.IsNullOrEmpty(field.Value.ToString()))
		    {
				return new FieldValidationResult(field, FieldValidationResult.ReasonForFailure.ProviderIdValueMissing);
		    }
		    return null;
	    }
    }
}
