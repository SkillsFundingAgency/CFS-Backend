using CalculateFunding.Models.Policy.TemplateBuilder;
using FluentValidation;

namespace CalculateFunding.Services.Policy.Validators
{
    public class TemplateContentUpdateCommandValidator : AbstractValidator<TemplateFundingLinesUpdateCommand>
    {
        public TemplateContentUpdateCommandValidator()
        {
            RuleFor(x => x.TemplateId).NotNull();
            RuleFor(x => x.TemplateFundingLinesJson).NotNull().NotEmpty();
        }
    }
}