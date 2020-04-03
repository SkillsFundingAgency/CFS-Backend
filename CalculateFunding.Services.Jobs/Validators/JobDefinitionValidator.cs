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
                .CustomAsync(async (jobDefinition, ctx, ct) =>
                {
                    string jobDefinitionId = jobDefinition.Id;

                    if (jobDefinitionId.IsNullOrWhitespace())
                    {
                        ctx.AddFailure(nameof(JobDefinition.Id), "You must supply a job definition id");
                    }
                    else
                    {
                        JobDefinition existingJobDefinition = await resiliencePolicies.JobDefinitionsRepository
                            .ExecuteAsync(() => jobDefinitions.GetJobDefinitionById(jobDefinitionId));

                        if (existingJobDefinition != null)
                        {
                            ctx.AddFailure(nameof(JobDefinition.Id), $"There is an existing job definition with the id {jobDefinitionId}. The id must be unique");
                        }
                    }

                    if (jobDefinition.MessageBusQueue.IsNullOrWhitespace() && jobDefinition.MessageBusTopic.IsNullOrWhitespace())
                    {
                        ctx.AddFailure(nameof(JobDefinition.MessageBusQueue), "You must supply a message bus queue if no topic supplied");
                        ctx.AddFailure(nameof(JobDefinition.MessageBusTopic), "You must supply a message bus topic if no queue supplied");
                    }
                });
        }
    }
}