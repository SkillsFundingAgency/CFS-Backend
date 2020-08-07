using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation;

namespace CalculateFunding.Services.Publishing.Validators
{
    public class PublishedProviderIdsValidator : AbstractValidator<string[]>, IPublishedProviderIdsServiceRequestValidator
    {
        public PublishedProviderIdsValidator()
        {
            RuleFor(_ => _)
                .NotEmpty()
                .WithMessage("No published provider Ids were provided");

            RuleForEach(_ => _)
                .NotEmpty()
                .WithMessage("No published provider Id was provided");
        }
    }
}
