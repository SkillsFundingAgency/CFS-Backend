using FluentValidation;

namespace CalculateFunding.Services.Publishing.Batches
{
    public class BatchUploadValidationRequestValidation : AbstractValidator<BatchUploadValidationRequest>
    {
        public BatchUploadValidationRequestValidation()
        {
            RuleFor(_ => _)
                .NotNull();
            RuleFor(_ => _.BatchId)
                .NotEmpty();
            RuleFor(_ => _.FundingPeriodId)
                .NotEmpty();
            RuleFor(_ => _.FundingStreamId)
                .NotEmpty();
            RuleFor(_ => _.SpecificationId)
                .NotEmpty();
        }
    }
}