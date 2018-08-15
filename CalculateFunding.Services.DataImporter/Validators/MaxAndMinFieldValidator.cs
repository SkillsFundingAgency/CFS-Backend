using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators.Extension;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.Validators
{
	public class MaxAndMinFieldValidator : IFieldValidator
	{
		public FieldValidationResult ValidateField(Field field)
		{
			FieldDefinition fieldDefinition = field.FieldDefinition;
			if (fieldDefinition.Maximum.HasValue)
				if (FieldTypeExtensions.CompareTo(fieldDefinition.Type, field.Value, fieldDefinition.Maximum.Value) > 0)
					return new FieldValidationResult(field, FieldValidationResult.ReasonForFailure.MaxOrMinValueExceeded);
			if (fieldDefinition.Minimum.HasValue)
				if (FieldTypeExtensions.CompareTo(fieldDefinition.Type, field.Value, fieldDefinition.Minimum.Value) < 0)
					return new FieldValidationResult(field, FieldValidationResult.ReasonForFailure.MaxOrMinValueExceeded);
			return null;
		}
	}
}