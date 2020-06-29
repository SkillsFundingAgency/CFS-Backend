using CalculateFunding.Models.Policy.TemplateBuilder;
using FluentValidation;

namespace CalculateFunding.Services.Policy.Validators
{
    public class TemplateDescriptionUpdateCommandValidator : AbstractValidator<TemplateDescriptionUpdateCommand>
    {
        public TemplateDescriptionUpdateCommandValidator()
        {
            RuleFor(x => x.TemplateId).NotNull();
            RuleFor(x => x.Description).Length(0, 1000);
        }
    }
}