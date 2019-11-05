using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Providers.Validators;
using FluentValidation;


namespace CalculateFunding.Services.Policy.Validators
{
    public class FundingStreamSaveModelValidator : AbstractValidator<FundingStreamSaveModel>
    {
        public FundingStreamSaveModelValidator()
        {
            RuleFor(_ => _.Id)
                .NotEmpty()
                .WithMessage("No id was provided for the FundingStream");

            RuleFor(_ => _.Name)
                .NotEmpty()
                .WithMessage("No name was provided for the FundingStream");

            RuleFor(_ => _.ShortName)
                .NotEmpty()
                .WithMessage("No short name was provided for the FundingStream");

        }
    }
}

