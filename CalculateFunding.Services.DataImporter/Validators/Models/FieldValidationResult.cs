namespace CalculateFunding.Services.DataImporter.Validators.Models
{
    public class FieldValidationResult
    {
	    public FieldValidationResult(Field fieldValidated, bool isValid, ReasonForFailure reasonOfFailure)
	    {
		    FieldValidated = fieldValidated;
		    ReasonOfFailure = reasonOfFailure;
	    }

	    public Field FieldValidated { get; set; }


	    public ReasonForFailure ReasonOfFailure { get; set; }

	    public enum ReasonForFailure
	    {
			None,
			ProviderIdMismatchWithServiceProvider

		 //   MissingRequiredFields,
		 //   DatatypeMismatch,
			//MaxOrMinExceeded,
			//ProviderIdMissing,
			//DuplicateProviderIdEntries,
		 //   MissingProviders,
		 //   NoProvidersInSystem
		}
    }		
}     