using CalculateFunding.Models.Policy;
using FluentValidation;

namespace CalculateFunding.Services.Providers.Validators
{
    public class FundingPeriodValidator : AbstractValidator<FundingPeriod>, IFundingPeriodValidator
    {
        public FundingPeriodValidator()
        {
            RuleFor(_ => _.Period)
                .NotEmpty()
                .WithMessage("No funding period was provided for the FundingPeriod");
        }
    }
}