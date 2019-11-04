using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Providers.Validators;
using FluentValidation;


namespace CalculateFunding.Services.Policy.Validators
{
    public class FundingPeriodJsonModelValidator : AbstractValidator<FundingPeriodsJsonModel>
    {
        public FundingPeriodJsonModelValidator()
        {
            RuleForEach(x => x.FundingPeriods).ChildRules(fundingPeriod => {              

                fundingPeriod.RuleFor(_ => _.Name)
                           .NotEmpty()
                           .WithMessage("No funding name was provided for the FundingPeriod");

                fundingPeriod.RuleFor(_ => _.StartDate)
                             .NotEmpty()
                             .WithMessage("No funding start date was provided for the FundingPeriod");

                fundingPeriod.RuleFor(_ => _.EndDate)
                            .NotEmpty()
                            .WithMessage("No funding end date was provided for the FundingPeriod");

                fundingPeriod.RuleFor(_ => _.Period)
                               .NotEmpty()
                               .WithMessage("No funding period was provided for the FundingPeriod");

                fundingPeriod.RuleFor(_ => _.Type)
                                .NotNull()
                                .WithMessage("Null funding type was provided for the FundingPeriod");

                fundingPeriod.RuleFor(_ => _.StartYear)
                               .NotEmpty()
                               .NotNull()
                               .WithMessage("No funding start year was provided for the FundingPeriod");

                fundingPeriod.RuleFor(_ => _.EndYear)
                               .NotEmpty()
                               .NotNull()
                               .WithMessage("No funding end year was provided for the FundingPeriod");
            });
        }
    }
}
