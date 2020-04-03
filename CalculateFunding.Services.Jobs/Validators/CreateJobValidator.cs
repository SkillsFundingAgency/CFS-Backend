using CalculateFunding.Models.Jobs;
using FluentValidation;
using System.Linq;

namespace CalculateFunding.Services.Jobs.Validators
{
    public class CreateJobValidator : AbstractValidator<CreateJobValidationModel>
    {
        public CreateJobValidator()
        {
            CascadeMode = CascadeMode.StopOnFirstFailure;

            RuleFor(model => model.JobCreateModel)
                .NotNull()
                .WithMessage("Null job create model was provided");

            RuleFor(model => model.JobDefinition)
               .NotNull()
               .WithMessage("Null job definition was provided");

            When(model => model.JobCreateModel != null && model.JobDefinition != null, () =>
            {
                RuleFor(model => model.JobCreateModel)
                .Custom((jobCreateModel, context) =>
                {
                    CreateJobValidationModel model = context.ParentContext.InstanceToValidate as CreateJobValidationModel;

                    if (model.JobDefinition.RequireMessageBody && string.IsNullOrWhiteSpace(jobCreateModel.MessageBody))
                    {
                        context.AddFailure($"A message body is required when using job definition: '{model.JobDefinition.Id}'");
                    }

                    if (!model.JobDefinition.RequireMessageProperties.IsNullOrEmpty())
                    {
                        if (jobCreateModel.Properties.IsNullOrEmpty())
                        {
                            context.AddFailure($"Message properties are required when using job definition: '{model.JobDefinition.Id}'");
                        }
                        else
                        {
                            foreach (string property in model.JobDefinition.RequireMessageProperties)
                            {
                                if (!jobCreateModel.Properties.ContainsKey(property))
                                {
                                    context.AddFailure($"Message property '{property}' is required when using job definition: '{model.JobDefinition.Id}'");
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(model.JobDefinition.SessionMessageProperty))
                    {
                        if (!jobCreateModel.Properties.ContainsKey(model.JobDefinition.SessionMessageProperty))
                        {
                            context.AddFailure($"Session Message property '{model.JobDefinition.SessionMessageProperty}' is required when using job definition: '{model.JobDefinition.Id}'");
                        }
                    }

                    if (model.JobDefinition.RequireSpecificationId && string.IsNullOrWhiteSpace(jobCreateModel.SpecificationId))
                    {
                        context.AddFailure($"A specification id is required when using job definition: '{model.JobDefinition.Id}'");
                    }

                    if (model.JobDefinition.RequireEntityId && string.IsNullOrWhiteSpace(jobCreateModel.Trigger?.EntityId))
                    {
                        context.AddFailure($"An entity id is required when using job definition: '{model.JobDefinition.Id}'");
                    }
                });

            });
        }
    }
}
