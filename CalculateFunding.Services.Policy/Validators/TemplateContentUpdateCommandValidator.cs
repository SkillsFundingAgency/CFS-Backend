using CalculateFunding.Models.Policy.TemplateBuilder;
using FluentValidation;

namespace CalculateFunding.Services.Policy.Validators
{
    public class TemplateContentUpdateCommandValidator : AbstractValidator<TemplateContentUpdateCommand>
    {
        public TemplateContentUpdateCommandValidator()
        {
            RuleFor(x => x.TemplateId).NotNull();
            RuleFor(x => x.TemplateJson).NotNull().NotEmpty();
        }
    }
}