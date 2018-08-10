using CalculateFunding.Models.Datasets.Schema;

namespace CalculateFunding.Services.DataImporter.Validators.Models
{
	public class HeaderValidationResult
	{
		public HeaderValidationResult(FieldDefinition fieldDefinitionValidated)
		{
			FieldDefinitionValidated = fieldDefinitionValidated;
		}

		public FieldDefinition FieldDefinitionValidated { get; set; }
	}
}