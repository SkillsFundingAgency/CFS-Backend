using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs.Interfaces;
using Newtonsoft.Json;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs
{
    public class QueueEditSpecificationJobActions: IQueueEditSpecificationJobActions
    {
        private readonly IJobManagement _jobManagement;
        private readonly IDatasetsApiClient _datasets;
        private readonly AsyncPolicy _datasetsPolicy;
        private readonly ILogger _logger;

        public QueueEditSpecificationJobActions(
            IJobManagement jobManagement,
            IDatasetsApiClient datasets,
            ISpecificationsResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies?.DatasetsApiClient, nameof(resiliencePolicies.DatasetsApiClient));
            Guard.ArgumentNotNull(datasets, nameof(datasets));

            _jobManagement = jobManagement;
            _logger = logger;
            _datasets = datasets;
            _datasetsPolicy = resiliencePolicies.DatasetsApiClient;
        }

        public async Task Run(
            SpecificationVersion specificationVersion, 
            Reference user, 
            string correlationId, 
            bool triggerProviderSnapshotDataLoadJob,
            bool triggerCalculationEngineRunJob)
        {
            string errorMessage = $"Unable to queue ProviderSnapshotDataLoadJob for specification - {specificationVersion.SpecificationId}";
            string triggerMessage = $"Assigning ProviderVersionId for specification: {specificationVersion.SpecificationId}";
            Reference fundingStream = specificationVersion.FundingStreams.FirstOrDefault();
            if (fundingStream != null && (specificationVersion.ProviderSource == Models.Providers.ProviderSource.FDZ || triggerProviderSnapshotDataLoadJob))
            {
                Job createProviderSnapshotDataLoadJob = await CreateJob(errorMessage,
                NewJobCreateModel(specificationVersion.SpecificationId,
                    triggerMessage,
                    JobConstants.DefinitionNames.ProviderSnapshotDataLoadJob,
                    correlationId,
                    user,
                    new Dictionary<string, string>
                    {
                        { "specification-id", specificationVersion.SpecificationId},
                        { "fundingstream-id", fundingStream.Id},
                        { "providerSanpshot-id", specificationVersion.ProviderSnapshotId?.ToString() },
                        { "disableQueueCalculationJob", (!triggerCalculationEngineRunJob).ToString()}
                    }));

                GuardAgainstNullJob(createProviderSnapshotDataLoadJob, errorMessage);
            }

            if(specificationVersion.ProviderSource == Models.Providers.ProviderSource.CFS && !string.IsNullOrWhiteSpace(specificationVersion.ProviderVersionId))
            {
                ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>> response =
                        await _datasetsPolicy.ExecuteAsync(() => _datasets.GetRelationshipsBySpecificationId(specificationVersion.SpecificationId));

                DatasetSpecificationRelationshipViewModel providerRelationship = response?.Content?.Where(_ => _.IsProviderData).FirstOrDefault();

                if (providerRelationship != null)
                {
                    ApiResponse<DatasetViewModel> datasetResponse =
                        await _datasetsPolicy.ExecuteAsync(() => _datasets.GetDatasetByDatasetId(providerRelationship.DatasetId));

                    if (datasetResponse?.StatusCode.IsSuccess() == false || datasetResponse?.Content == null)
                    {
                        errorMessage = $"Unable to retrieve dataset for specification {specificationVersion.SpecificationId}";
                        _logger.Error(errorMessage);
                        throw new Exception(errorMessage);
                    }

                    errorMessage = $"Unable to queue MapScopedDatasetJob for specification - {specificationVersion.SpecificationId}";
                    Job mapScopedDatasetJob = await CreateJob(errorMessage,
                        NewJobCreateModel(specificationVersion.SpecificationId,
                        triggerMessage,
                        JobConstants.DefinitionNames.MapScopedDatasetJob,
                        correlationId,
                        user,
                        new Dictionary<string, string>
                        {
                            { "specification-id", specificationVersion.SpecificationId},
                            { "provider-cache-key", $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationVersion.SpecificationId}"},
                            { "specification-summary-cache-key", $"{CacheKeys.SpecificationSummaryById}{specificationVersion.SpecificationId}"},
                            { "disableQueueCalculationJob", (!triggerCalculationEngineRunJob).ToString()},
                        }));

                    GuardAgainstNullJob(mapScopedDatasetJob, errorMessage);

                    errorMessage = $"Unable to queue MapDatasetJob for specification - {specificationVersion.SpecificationId}";
                    Job mapDataset = await CreateJob(errorMessage, NewJobCreateModel(specificationVersion.SpecificationId,
                        triggerMessage,
                        JobConstants.DefinitionNames.MapDatasetJob,
                        correlationId,
                        user,
                        new Dictionary<string, string>
                        {
                            { "specification-id", specificationVersion.SpecificationId },
                            { "relationship-id", providerRelationship.Id },
                            { "user-id", user?.Id},
                            { "user-name", user?.Name},
                            { "parentJobId", mapScopedDatasetJob.Id },
                            { "disableQueueCalculationJob", (!triggerCalculationEngineRunJob).ToString()},
                        },
                        mapScopedDatasetJob.Id,
                        messageBody:JsonConvert.SerializeObject(datasetResponse.Content)));

                    GuardAgainstNullJob(mapDataset, errorMessage);
                }
            }
        }

        private JobCreateModel NewJobCreateModel(string specificationId,
            string message,
            string jobDefinitionId,
            string correlationId,
            Reference user,
            IDictionary<string, string> properties,
            string parentJobId = null,
            int? itemCount = null,
            string messageBody = null)
        {
            return new JobCreateModel
            {
                Trigger = new Trigger
                {
                    EntityId = specificationId,
                    EntityType = nameof(Specification),
                    Message = message
                },
                InvokerUserId = user.Id,
                InvokerUserDisplayName = user.Name,
                JobDefinitionId = jobDefinitionId,
                ParentJobId = parentJobId,
                SpecificationId = specificationId,
                Properties = properties,
                CorrelationId = correlationId,
                ItemCount = itemCount,
                MessageBody = messageBody
            };
        }

        private async Task<Job> CreateJob(string errorMessage, JobCreateModel createModel)
        {
            try
            {
                Job job = await _jobManagement.QueueJob(createModel);

                return job;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, errorMessage);

                throw new Exception(errorMessage);
            }
        }

        private void GuardAgainstNullJob(Job job, string message)
        {
            if (job != null) return;

            _logger.Error(message);

            throw new Exception(message);
        }
    }
}
