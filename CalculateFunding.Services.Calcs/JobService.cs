using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class JobService : ProcessingService, IJobService
    {
        private readonly IJobManagement _jobManagement;
        private readonly ILogger _logger;

        public JobService(IJobManagement jobManagement, ILogger logger)
        {
            _jobManagement = jobManagement;
            _logger = logger;
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            JobSummary jobSummary = message.GetPayloadAsInstanceOf<JobSummary>();

            if (jobSummary.CompletionStatus == CompletionStatus.Succeeded && jobSummary.JobType == JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob)
            {
                JobCreateModel jobCreateModel = new JobCreateModel
                {
                    JobDefinitionId = JobConstants.DefinitionNames.CreateInstructAllocationJob,
                    InvokerUserDisplayName = jobSummary.InvokerUserDisplayName,
                    InvokerUserId = jobSummary.InvokerUserId,
                    CorrelationId = message.GetCorrelationId(),
                    SpecificationId = jobSummary.SpecificationId,
                    Properties = new Dictionary<string, string>
                                {
                                    { "specification-id", jobSummary.SpecificationId }
                                },
                    Trigger = jobSummary.Trigger
                };

                Job newJob = await _jobManagement.QueueJob(jobCreateModel);

                if(newJob == null)
                {
                    _logger.Error($"Failed to create new job of type: '{JobConstants.DefinitionNames.CreateInstructAllocationJob}'");

                    throw new Exception($"Failed to create new job of type: '{JobConstants.DefinitionNames.CreateInstructAllocationJob}'");
                }

                _logger.Information($"Created new job of type: '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' with id: '{newJob.Id}'");
            }
        }
    }
}
