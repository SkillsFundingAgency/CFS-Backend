using FluentValidation;

namespace CalculateFunding.Services.Publishing.Validators
{
    public class PublishProviderValidator : AbstractValidator<string>
    {
        public PublishProviderValidator()
        {
            RuleFor(_ => _)
            .NotEmpty()
            .WithMessage("No provider Id was provided");
        }
    }
}
