using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.DataImporter.Validators;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter
{
	public class DatatypeMismatchFieldValidator : IFieldValidator
	{
		public bool PreValidate(Field field)
		{
			return !string.IsNullOrEmpty(field.Value?.ToString());
		}

		public FieldValidationResult ValidateField(Field field)
		{
			FieldDefinition fieldDefinition = field.FieldDefinition;
			if (!PreValidate(field))
			{
				return null;
			}
			if (fieldDefinition.Type == FieldType.Boolean && field.Value.ToString().TryParseBoolean() != null ||
			    fieldDefinition.Type == FieldType.Char && field.Value.ToString().TryParseChar() != null ||
			    fieldDefinition.Type == FieldType.Byte && field.Value.ToString().TryParseByte() != null ||
			    fieldDefinition.Type == FieldType.Integer && field.Value.ToString().TryParseInt64() != null ||
			    fieldDefinition.Type == FieldType.Float && field.Value.ToString().TryParseFloat() != null ||
			    fieldDefinition.Type == FieldType.Decimal && field.Value.ToString().TryParseDecimal() != null ||
                fieldDefinition.Type == FieldType.NullableOfDecimal && field.Value.ToString().TryParseNullable(out decimal? parsedDecimal) ||
                fieldDefinition.Type == FieldType.NullableOfInteger && field.Value.ToString().TryParseNullable(out int? parsedInt) ||
                fieldDefinition.Type == FieldType.DateTime && field.Value.ToString().TryParseDateTime() != null ||
				fieldDefinition.Type == FieldType.String && !string.IsNullOrEmpty(field.Value?.ToString()))
			{
				return null;
			}

			return new FieldValidationResult(field, DatasetCellReasonForFailure.DataTypeMismatch);
		}
	}
}