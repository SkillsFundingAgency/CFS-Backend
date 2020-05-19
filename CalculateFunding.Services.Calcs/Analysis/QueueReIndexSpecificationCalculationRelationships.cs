using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
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
        private readonly IJobManagement _jobManagement;

        public QueueReIndexSpecificationCalculationRelationships(IJobManagement jobManagement)
        {
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));

            _jobManagement = jobManagement;
        }

        public async Task<IActionResult> QueueForSpecification(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            await _jobManagement.QueueJob(new JobCreateModel
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
            });
            
            return new OkResult();
        }
    }
}