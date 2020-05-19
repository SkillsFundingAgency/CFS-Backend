using System;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using FluentValidation;

namespace CalculateFunding.Services.Jobs.Validators
{
    public class JobDefinitionValidator : AbstractValidator<JobDefinition>
    {
        public JobDefinitionValidator(IJobDefinitionsRepository jobDefinitions,
            IJobsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(jobDefinitions, nameof(jobDefinitions));
            Guard.ArgumentNotNull(resiliencePolicies?.JobDefinitionsRepository, nameof(resiliencePolicies.JobDefinitionsRepository));

            RuleFor(_ => _)
                .Custom((jobDefinition, ctx) =>
                {
                    string jobDefinitionId = jobDefinition.Id;

                    if (jobDefinitionId.IsNullOrWhitespace())
                    {
                        ctx.AddFailure(nameof(JobDefinition.Id), "You must supply a job definition id");
                    }
                });
        }
    }
}