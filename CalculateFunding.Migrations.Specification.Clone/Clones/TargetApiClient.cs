using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Utility;
using CalculateFunding.Migrations.Specification.Clone.Helpers;
using CalculateFunding.Services.Core.Constants;
using Polly;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Migrations.Specification.Clone.Clones
{
    public class TargetApiClient : ITargetApiClient
    {
        private readonly ILogger _logger;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IJobsApiClient _jobsApiClient;
        private readonly ICalculationsApiClient _calculationsApiClient;
        private readonly IDatasetsApiClient _datasetsApiClient;

        private readonly AsyncPolicy _specificationsPolicy;
        private readonly AsyncPolicy _jobsPolicy;
        private readonly AsyncPolicy _calcsPolicy;
        private readonly AsyncPolicy _datasetsPolicy;

        public TargetApiClient(
            ILogger logger,
            IBatchCloneResiliencePolicies batchCloneResiliencePolicies,
            ISpecificationsApiClient specificationsApiClient,
            IJobsApiClient jobsApiClient,
            ICalculationsApiClient calculationsApiClient,
            IDatasetsApiClient datasetsApiClient)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));

            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(datasetsApiClient, nameof(datasetsApiClient));

            Guard.ArgumentNotNull(batchCloneResiliencePolicies, nameof(batchCloneResiliencePolicies));
            Guard.ArgumentNotNull(batchCloneResiliencePolicies.SpecificationsApiClient, nameof(batchCloneResiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(batchCloneResiliencePolicies.JobsApiClient, nameof(batchCloneResiliencePolicies.JobsApiClient));
            Guard.ArgumentNotNull(batchCloneResiliencePolicies.CalcsApiClient, nameof(batchCloneResiliencePolicies.CalcsApiClient));
            Guard.ArgumentNotNull(batchCloneResiliencePolicies.DatasetsApiClient, nameof(batchCloneResiliencePolicies.DatasetsApiClient));

            _logger = logger;
            _specificationsApiClient = specificationsApiClient;
            _jobsApiClient = jobsApiClient;
            _calculationsApiClient = calculationsApiClient;
            _datasetsApiClient = datasetsApiClient;

            _specificationsPolicy = batchCloneResiliencePolicies.SpecificationsApiClient;
            _jobsPolicy = batchCloneResiliencePolicies.JobsApiClient;
            _calcsPolicy = batchCloneResiliencePolicies.CalcsApiClient;
            _datasetsPolicy = batchCloneResiliencePolicies.DatasetsApiClient;
        }

        public async Task<SpecificationSummary> CreateSpecification(CreateSpecificationModel createSpecificationModel)
        {
            ValidatedApiResponse<SpecificationSummary> cloneSpecificationSummaryResponse =
                await _specificationsPolicy.ExecuteAsync(() => _specificationsApiClient.CreateSpecification(createSpecificationModel));
            cloneSpecificationSummaryResponse.ValidateApiResponse(_logger, $"Specification Clone operation failed for CreateSpecificationModel={createSpecificationModel.AsJson()}");
            return cloneSpecificationSummaryResponse.Content;
        }

        public async Task<IDictionary<string, JobSummary>> GetLatestJobsForSpecification(string specificationId, params string[] jobDefinitionIds)
        {
            ApiResponse<IDictionary<string, JobSummary>> latestJobsForSpecificationResponse =
                await _jobsPolicy.ExecuteAsync(() => _jobsApiClient.GetLatestJobsForSpecification(specificationId, jobDefinitionIds));
            latestJobsForSpecificationResponse.ValidateApiResponse(_logger, $"GetLatestJobsForSpecification operation failed for SpecificationId={specificationId} and {nameof(JobConstants.DefinitionNames.AssignTemplateCalculationsJob)}");
            return latestJobsForSpecificationResponse.Content;
        }

        public async Task<Calculation> CreateCalculation(
            string specificationId, 
            CalculationCreateModel calculationCreateModel,
            bool skipCalcRun,
            bool skipQueueCodeContextCacheUpdate,
            bool overrideCreateModelAuthor)
        {
            ValidatedApiResponse<Calculation> newAdditionalCalculationResponse =
                    await _calcsPolicy.ExecuteAsync(() => _calculationsApiClient.CreateCalculation(
                        specificationId, 
                        calculationCreateModel, 
                        skipCalcRun: skipCalcRun, 
                        skipQueueCodeContextCacheUpdate: skipQueueCodeContextCacheUpdate,
                        overrideCreateModelAuthor: overrideCreateModelAuthor));
            newAdditionalCalculationResponse.ValidateApiResponse(_logger, $"Create clone additional calculation failed with StatusCode={newAdditionalCalculationResponse.StatusCode} " +
                        $"and ErrorMessage={newAdditionalCalculationResponse.Message} and Dictionary={newAdditionalCalculationResponse.ModelState.AsJson()} with input " +
                        $"{calculationCreateModel.AsJson()}");
            return newAdditionalCalculationResponse.Content;
        }

        public async Task<Calculation> EditCalculationWithSkipInstruct(string specificationId, string calculationId, CalculationEditModel calculationEditModel)
        {
            ValidatedApiResponse<Calculation> editCalculationResponse =
                    await _calcsPolicy.ExecuteAsync(() => _calculationsApiClient.EditCalculationWithSkipInstruct(specificationId, calculationId, calculationEditModel));
            editCalculationResponse.ValidateApiResponse(_logger, $"Edit calculation failed with StatusCode={editCalculationResponse.StatusCode} " +
                        $"and ErrorMessage={editCalculationResponse.Message} and Dictionary={editCalculationResponse.ModelState.AsJson()} with input " +
                        $"{calculationEditModel.AsJson()}");
            return editCalculationResponse.Content;
        }

        public async Task<Common.ApiClient.Calcs.Models.Job> QueueCodeContextUpdate(string specificationId)
        {
            ApiResponse<Common.ApiClient.Calcs.Models.Job> queueCodeContextUpdateJobResponse =
                    await _calcsPolicy.ExecuteAsync(() => _calculationsApiClient.QueueCodeContextUpdate(specificationId));
            queueCodeContextUpdateJobResponse.ValidateApiResponse(_logger, $"{nameof(QueueCodeContextUpdate)} operation failed for SpecificationId={specificationId}");

            return queueCodeContextUpdateJobResponse.Content;
        }

        public async Task<Common.ApiClient.Calcs.Models.Job> QueueCalculationRun(string specificationId, QueueCalculationRunModel model)
        {
            ApiResponse<Common.ApiClient.Calcs.Models.Job> queueCalculationRunJobResponse =
                    await _calcsPolicy.ExecuteAsync(() => _calculationsApiClient.QueueCalculationRun(specificationId, model));
            queueCalculationRunJobResponse.ValidateApiResponse(_logger, $"{nameof(QueueCalculationRun)} operation failed for SpecificationId={specificationId}");

            return queueCalculationRunJobResponse.Content;
        }

        public async Task<DefinitionSpecificationRelationship> CreateRelationship(CreateDefinitionSpecificationRelationshipModel createDefinitionSpecificationRelationshipModel)
        {
            ApiResponse<DefinitionSpecificationRelationship> definitionSpecificationRelationshipResponse =
                    await _datasetsPolicy.ExecuteAsync(() => _datasetsApiClient.CreateRelationship(createDefinitionSpecificationRelationshipModel));
            string errorMessage = $"Create clone dataset relationship failed with StatusCode={definitionSpecificationRelationshipResponse.StatusCode} " +
                    $"and ErrorMessage={definitionSpecificationRelationshipResponse.Message} for input " +
                    $"{createDefinitionSpecificationRelationshipModel.AsJson()}";

            definitionSpecificationRelationshipResponse.ValidateApiResponse(_logger, errorMessage);

            return definitionSpecificationRelationshipResponse.Content;
        }

        public async Task<JobViewModel> GetJobById(string jobId)
        {
            ApiResponse<JobViewModel> jobResponse = await _jobsPolicy.ExecuteAsync(() => _jobsApiClient.GetJobById(jobId));

            return jobResponse?.Content;
        }

        public async Task<IEnumerable<Calculation>> GetCalculationsForSpecification(string specificationId)
        {
            ApiResponse<IEnumerable<Calculation>> calculationsResponse =
                await _calcsPolicy.ExecuteAsync(() => _calculationsApiClient.GetCalculationsForSpecification(specificationId));
            calculationsResponse.ValidateApiResponse(_logger, $"{nameof(GetCalculationsForSpecification)}operation failed for SpecificationId={specificationId}");
            return calculationsResponse.Content;
        }
    }
}
