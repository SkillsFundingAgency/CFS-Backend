
using CalculateFunding.Models.Messages;
using FluentValidation;

namespace CalculateFunding.Services.Validators
{
    public class AssignDefinitionRelationshipMessageValidator : AbstractValidator<AssignDefinitionRelationshipMessage>
    {
        public AssignDefinitionRelationshipMessageValidator()
        {
            RuleFor(model => model.RelationshipId)
               .NotEmpty()
               .WithMessage("A null or empty relationship id was provided");

            RuleFor(model => model.SpecificationId)
               .NotEmpty()
               .WithMessage("A null or empty specification id was provided");
        }
    }
}
