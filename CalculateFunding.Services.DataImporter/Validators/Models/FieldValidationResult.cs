namespace CalculateFunding.Services.DataImporter.Validators.Models
{
    public class FieldValidationResult
    {
	    public FieldValidationResult(Field fieldValidated, DatasetCellReasonForFailure reasonOfFailure)
	    {
		    FieldValidated = fieldValidated;
		    ReasonOfFailure = reasonOfFailure;
	    }

	    public Field FieldValidated { get; set; }


	    public DatasetCellReasonForFailure ReasonOfFailure { get; set; }
    }
}     