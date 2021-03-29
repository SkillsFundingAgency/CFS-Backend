namespace CalculateFunding.Services.DataImporter.Validators.Models
{
    public class FieldValidationResult
    {
	    public FieldValidationResult(Field fieldValidated, ReasonForFailure reasonOfFailure)
	    {
		    FieldValidated = fieldValidated;
		    ReasonOfFailure = reasonOfFailure;
	    }

	    public Field FieldValidated { get; set; }


	    public ReasonForFailure ReasonOfFailure { get; set; }

	    public enum ReasonForFailure
	    {
			None,
			DataTypeMismatch,
			MaxOrMinValueExceeded,
			ProviderIdValueMissing,
			DuplicateEntriesInTheProviderIdColumn,
			ProviderIdMismatchWithServiceProvider,
			NewProviderMissingAllDataSchemaFields,
			ExtraHeaderField
		}
    }		
}     