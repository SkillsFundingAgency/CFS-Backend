using CalculateFunding.Models.Policy.TemplateBuilder;
using FluentValidation;

namespace CalculateFunding.Services.Policy.Validators
{
    public class TemplateMetadataUpdateCommandValidator : AbstractValidator<TemplateMetadataUpdateCommand>
    {
        public TemplateMetadataUpdateCommandValidator()
        {
            RuleFor(x => x.TemplateId).NotNull();
            RuleFor(x => x.Name).NotNull();
            RuleFor(x => x.Name).Length(3, 200);
            RuleFor(x => x.Description).Length(0, 1000);
        }
    }
}