using CalculateFunding.Models.Policy.TemplateBuilder;
using FluentValidation;

namespace CalculateFunding.Services.Policy.Validators
{
    public class TemplateCreateCommandValidator : AbstractValidator<TemplateCreateCommand>
    {
        public TemplateCreateCommandValidator()
        {
            RuleFor(x => x.Name).NotNull();
            RuleFor(x => x.Name).Length(3, 200);
            RuleFor(x => x.Description).Length(0, 1000);
            RuleFor(x => x.FundingStreamId).NotEmpty().Length(3, 50);
        }
    }
}