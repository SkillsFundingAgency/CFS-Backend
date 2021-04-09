using CalculateFunding.Models.Datasets.Schema;

namespace CalculateFunding.Services.DataImporter.Validators.Models
{
	public class HeaderValidationResult
	{
		public HeaderValidationResult(FieldDefinition fieldDefinition)
		{
			FieldDefinition = fieldDefinition;
		}

		public FieldDefinition FieldDefinition { get; set; }
		
		public DatasetCellReasonForFailure ReasonForFailure { get; set; }
		
		public bool HasBackgroundKeyColour { get; set; }

		public static HeaderValidationResult CreateResultRequiringBackgroundColourKey(FieldDefinition fieldDefinition,
			DatasetCellReasonForFailure reasonForFailure)
			=> new HeaderValidationResult(fieldDefinition)
			{
				ReasonForFailure = reasonForFailure,
				HasBackgroundKeyColour = true
			};
	}
}