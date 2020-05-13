using CalculateFunding.Models.Policy.TemplateBuilder;
using FluentValidation;

namespace CalculateFunding.Services.Policy.Validators
{
    public class FindTemplateVersionQueryValidator : AbstractValidator<FindTemplateVersionQuery>
    {
        public FindTemplateVersionQueryValidator()
        {
            RuleFor(x => x.FundingStreamId).NotEmpty().Length(3, 50);
            RuleFor(x => x.FundingPeriodId).NotEmpty().Length(3, 50);
        }
    }
}