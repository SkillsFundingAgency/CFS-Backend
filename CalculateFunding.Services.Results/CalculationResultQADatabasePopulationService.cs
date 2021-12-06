﻿using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Models;
using CalculateFunding.Services.Results.SqlExport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class CalculationResultQADatabasePopulationService : JobProcessingService, ICalculationResultQADatabasePopulationService
    {
        private readonly AsyncPolicy _jobsPolicy;
        private readonly IJobManagement _jobs;
        private readonly IQaSchemaService _schema;

        public CalculationResultQADatabasePopulationService(
            IQaSchemaService schema,
            IResultsResiliencePolicies resiliencePolicies,
            IJobManagement jobs,
            ILogger logger)
            : base(jobs, logger)
        {
            Guard.ArgumentNotNull(schema, nameof(schema));
            Guard.ArgumentNotNull(resiliencePolicies?.JobsApiClient, nameof(resiliencePolicies.JobsApiClient));

            _schema = schema;
            _jobsPolicy = resiliencePolicies.JobsApiClient;
            _jobs = jobs;
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            PopulateCalculationResultQADatabaseRequest mergeRequest = message.GetPayloadAsInstanceOf<PopulateCalculationResultQADatabaseRequest>();

            await PopulateCalculationResultQADatabase(mergeRequest);
        }

        public async Task<IActionResult> QueueCalculationResultQADatabasePopulationJob(
            PopulateCalculationResultQADatabaseRequest populateCalculationResultQADatabaseRequest,
            Reference user,
            string correlationId)
        {
            Guard.ArgumentNotNull(populateCalculationResultQADatabaseRequest, nameof(populateCalculationResultQADatabaseRequest));

            JobCreateModel job = new JobCreateModel
            {
                JobDefinitionId = JobConstants.DefinitionNames.PopulateCalculationResultsQaDatabaseJob,
                InvokerUserId = user?.Id,
                InvokerUserDisplayName = user?.Name,
                CorrelationId = correlationId,
                Trigger = new Trigger
                {
                    Message = "Triggering Populate Calculation Result QA Database Request as per API request",
                    EntityType = "Specification",
                    EntityId = populateCalculationResultQADatabaseRequest.SpecificationId
                },
                MessageBody = populateCalculationResultQADatabaseRequest.AsJson(),
                SpecificationId = populateCalculationResultQADatabaseRequest.SpecificationId,
                Properties = new Dictionary<string, string>
                {
                    {"specification-id", populateCalculationResultQADatabaseRequest.SpecificationId},
                },
            };

            return new OkObjectResult(await _jobsPolicy.ExecuteAsync(() => _jobs.QueueJob(job)));
        }

        public async Task PopulateCalculationResultQADatabase(PopulateCalculationResultQADatabaseRequest populateCalculationResultQADatabaseRequest)
        {
            Guard.ArgumentNotNull(populateCalculationResultQADatabaseRequest, nameof(populateCalculationResultQADatabaseRequest));

            await _schema.ReCreateTablesForSpecification(populateCalculationResultQADatabaseRequest.SpecificationId);
        }
    }

}
