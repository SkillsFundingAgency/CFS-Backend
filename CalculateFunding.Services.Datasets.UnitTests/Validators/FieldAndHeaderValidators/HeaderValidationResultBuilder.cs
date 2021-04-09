using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Validators.FieldAndHeaderValidators
{
    public class HeaderValidationResultBuilder : TestEntityBuilder
    {
        private FieldDefinition _fieldDefinition;
        private DatasetCellReasonForFailure _reasonForFailure;
        private bool _hasBackgroundKeyColour;

        public HeaderValidationResultBuilder WithFieldDefinition(FieldDefinition fieldDefinition)
        {
            _fieldDefinition = fieldDefinition;

            return this;
        }

        public HeaderValidationResultBuilder WithReasonForFailure(DatasetCellReasonForFailure reasonForFailure)
        {
            _reasonForFailure = reasonForFailure;

            return this;
        }

        public HeaderValidationResultBuilder WithHasBackgroundKeyColour(bool hasBackgroundKeyColour)
        {
            _hasBackgroundKeyColour = hasBackgroundKeyColour;

            return this;
        }
        
        public HeaderValidationResult Build()
        {
            return new HeaderValidationResult(_fieldDefinition)
            {
                ReasonForFailure = _reasonForFailure,
                HasBackgroundKeyColour = _hasBackgroundKeyColour,
            };
        }
    }
}