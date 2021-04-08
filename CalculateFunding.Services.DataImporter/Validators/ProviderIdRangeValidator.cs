using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.Validators
{
    public class ProviderIdRangeValidator : IFieldValidator
    {
        private const int MinProviderIdValue = 1000000;
        private const int MaxProviderIdValue = 100000000;

        private bool PreValidation(Field field)
        {
            return field.FieldDefinition.IdentifierFieldType.HasValue 
                && field.FieldDefinition.IdentifierFieldType == IdentifierFieldType.UKPRN;
        }

        public FieldValidationResult ValidateField(Field field)
        {
            if (!PreValidation(field))
            {
                return null;
            }

            bool parsed = int.TryParse(field.Value.ToString(), out int fieldValue);

            if (!parsed)
            {
                return new FieldValidationResult(field, FieldValidationResult.ReasonForFailure.DataTypeMismatch);
            }

            if(fieldValue < MinProviderIdValue || fieldValue > MaxProviderIdValue)
            {
                return new FieldValidationResult(field, FieldValidationResult.ReasonForFailure.ProviderIdNotInCorrectFormat);
            }

            return null;
        }
    }
}
