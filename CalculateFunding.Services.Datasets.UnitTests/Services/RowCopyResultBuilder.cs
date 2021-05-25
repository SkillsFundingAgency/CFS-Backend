using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class RowCopyResultBuilder : TestEntityBuilder
    {
        private ProviderConverter _eligibleConverter;
        private RowCopyOutcome _outcome;
        private string _validationMessage;

        public RowCopyResultBuilder WithEligibleConverter(ProviderConverter eligibleConverter)
        {
            _eligibleConverter = eligibleConverter;

            return this;
        }

        public RowCopyResultBuilder WithOutcome(RowCopyOutcome outcome)
        {
            _outcome = outcome;

            return this;
        }

        public RowCopyResultBuilder WithValidationMessage(string validationMessage)
        {
            _validationMessage = validationMessage;

            return this;
        }

        public RowCopyResult Build()
        {
            return new RowCopyResult()
            {
                EligibleConverter = _eligibleConverter,
                Outcome = _outcome,
                ValidationMessage = _validationMessage,
            };
        }
    }
}
