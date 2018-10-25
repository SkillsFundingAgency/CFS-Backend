using CalculateFunding.Models.Users;
using FluentValidation;

namespace CalculateFunding.Services.Users.Validators
{
    public class UserCreateModelValidator : AbstractValidator<UserCreateModel>
    {
        public UserCreateModelValidator()
        {
            RuleFor(model => model.Username)
               .NotEmpty()
               .WithMessage("A null or empty username was provided");

            RuleFor(model => model.Name)
               .NotEmpty()
               .WithMessage("A null or empty name was provided");
        }
    }
}
