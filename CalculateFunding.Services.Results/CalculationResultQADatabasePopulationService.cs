using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
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
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class CalculationResultQADatabasePopulationService : JobProcessingService, ICalculationResultQADatabasePopulationService
    {
        private readonly AsyncPolicy _jobsPolicy;
        private readonly AsyncPolicy _providersApiPolicy;
        private IProvidersApiClient _providersApiClient;
        private readonly IJobManagement _jobs;
        private readonly IQaSchemaService _schema;
        private readonly ISqlImporter _import;
        private readonly ILogger _logger;

        public CalculationResultQADatabasePopulationService(
            IQaSchemaService schema,
            IProvidersApiClient providersApiClient,
            IResultsResiliencePolicies resiliencePolicies,
            IJobManagement jobs,
            ILogger logger,
            ISqlImporter import)
            : base(jobs, logger)
        {
            Guard.ArgumentNotNull(schema, nameof(schema));
            Guard.ArgumentNotNull(resiliencePolicies?.JobsApiClient, nameof(resiliencePolicies.JobsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.ProvidersApiClient, nameof(resiliencePolicies.ProvidersApiClient));
            Guard.ArgumentNotNull(providersApiClient, nameof(providersApiClient));
            Guard.ArgumentNotNull(import, nameof(import));

            _schema = schema;
            _jobsPolicy = resiliencePolicies.JobsApiClient;
            _jobs = jobs;
            _providersApiClient = providersApiClient;
            _providersApiPolicy = resiliencePolicies.ProvidersApiClient;
            _import = import;
            _logger = logger;
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

            IDictionary<string, JobSummary> jobSummaries = await _jobsPolicy.ExecuteAsync(() => 
                _jobs.GetLatestJobsForSpecification(populateCalculationResultQADatabaseRequest.SpecificationId, new[] { JobConstants.DefinitionNames.PopulateCalculationResultsQaDatabaseJob }));

            if (jobSummaries != null 
                && jobSummaries.ContainsKey(JobConstants.DefinitionNames.PopulateCalculationResultsQaDatabaseJob) 
                && jobSummaries[JobConstants.DefinitionNames.PopulateCalculationResultsQaDatabaseJob] != null
                && (jobSummaries[JobConstants.DefinitionNames.PopulateCalculationResultsQaDatabaseJob].RunningStatus == RunningStatus.InProgress 
                    || jobSummaries[JobConstants.DefinitionNames.PopulateCalculationResultsQaDatabaseJob].RunningStatus == RunningStatus.Queued))
            {
                string errorMessage = 
                    $"There is an existing {JobConstants.DefinitionNames.PopulateCalculationResultsQaDatabaseJob} job running for Specification {populateCalculationResultQADatabaseRequest.SpecificationId}. Please wait for that job to complete.";

                return new BadRequestObjectResult(errorMessage);
            }

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

            ApiResponse<IEnumerable<string>> providersResult =  await _providersApiPolicy.ExecuteAsync(() => _providersApiClient.GetScopedProviderIds(populateCalculationResultQADatabaseRequest.SpecificationId));

            if (providersResult?.Content == null)
            {
                string errorMessage = $"Specification: {populateCalculationResultQADatabaseRequest.SpecificationId} scoped providers not found";
                _logger.Error(errorMessage);

                throw new NonRetriableException(errorMessage);
            }

            HashSet<string> providers = providersResult.Content.ToHashSet();

            await _import.ImportData(providers, populateCalculationResultQADatabaseRequest.SpecificationId);
        }
    }

}
