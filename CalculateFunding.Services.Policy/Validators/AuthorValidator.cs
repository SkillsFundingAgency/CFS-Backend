using CalculateFunding.Common.Models;
using FluentValidation;

namespace CalculateFunding.Services.Policy.Validators
{
    public class AuthorValidator : AbstractValidator<Reference>
    {
        public AuthorValidator()
        {
            RuleFor(x => x.Id).NotNull().NotEmpty();
            RuleFor(x => x.Name).NotNull();
            RuleFor(x => x.Name).Length(3, 200);
        }
    }
}