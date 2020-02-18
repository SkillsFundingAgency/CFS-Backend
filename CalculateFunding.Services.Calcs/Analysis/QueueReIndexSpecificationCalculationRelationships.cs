using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using Microsoft.AspNetCore.Mvc;
using Polly;

namespace CalculateFunding.Services.Calcs.Analysis
{
    public class QueueReIndexSpecificationCalculationRelationships : IQueueReIndexSpecificationCalculationRelationships
    {
        private readonly Policy _resilience;
        private readonly IJobsApiClient _jobs;

        public QueueReIndexSpecificationCalculationRelationships(IJobsApiClient jobs,
            ICalcsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(resiliencePolicies?.JobsApiClient, nameof(resiliencePolicies.JobsApiClient));
            
            _jobs = jobs;
            _resilience = resiliencePolicies.JobsApiClient;
        }

        public async Task<IActionResult> QueueForSpecification(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            await _resilience.ExecuteAsync(() => _jobs.CreateJob(new JobCreateModel
            {
                JobDefinitionId = JobConstants.DefinitionNames.ReIndexSpecificationCalculationRelationshipsJob,
                SpecificationId = specificationId,
                Trigger = new Trigger
                {
                    EntityId = specificationId,
                    EntityType = nameof(Specification),
                    Message = "Triggered for specification changes"
                },
                Properties = new Dictionary<string, string>
                {
                    {"specification-id", specificationId}
                }
            }));
            
            return new OkResult();
        }
    }
}