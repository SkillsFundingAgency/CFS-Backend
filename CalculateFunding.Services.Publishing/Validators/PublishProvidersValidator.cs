using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation;

namespace CalculateFunding.Services.Publishing.Validators
{
    public class PublishProvidersValidator : AbstractValidator<string[]>, IProviderIdsServiceRequestValidator
    {
        public PublishProvidersValidator()
        {
            RuleFor(_ => _)
                .NotEmpty()
                .WithMessage("No provider Ids were provided");

            RuleForEach(_ => _)
                .SetValidator(new PublishProviderValidator());
        }
    }
}
