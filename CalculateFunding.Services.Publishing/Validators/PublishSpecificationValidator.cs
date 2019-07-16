using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation;

namespace CalculateFunding.Services.Publishing.Validators
{
    public class PublishSpecificationValidator : AbstractValidator<string>, IPublishSpecificationValidator
    {
        public PublishSpecificationValidator()
        {
            RuleFor(_ => _)
                .NotEmpty()
                .WithMessage("No specification Id was provided");
        }
    }
}