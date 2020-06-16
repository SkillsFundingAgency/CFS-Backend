using CalculateFunding.Models.Policy.TemplateBuilder;
using FluentValidation;

namespace CalculateFunding.Services.Policy.Validators
{
    public class TemplatePublishCommandValidator : AbstractValidator<TemplatePublishCommand>
    {
        public TemplatePublishCommandValidator()
        {
            RuleFor(x => x.TemplateId).NotNull();
            RuleFor(x => x.Author).NotNull();
            RuleFor(x => x.Note).NotNull();
            RuleFor(x => x.Note).Length(0, 1000);
        }
    }
}