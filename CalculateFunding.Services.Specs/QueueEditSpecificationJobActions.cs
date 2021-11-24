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
        private readonly ISpecificationTemplateVersionChangedHandler _templateVersionChangedHandler;
        private readonly ILogger _logger;
        private const string EditSpecificationJobOutCome = "No change";

        public QueueEditSpecificationJobActions(
            IJobManagement jobManagement,
            IDatasetsApiClient datasets,
            ISpecificationsResiliencePolicies resiliencePolicies,
            ISpecificationTemplateVersionChangedHandler templateVersionChangedHandler,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies?.DatasetsApiClient, nameof(resiliencePolicies.DatasetsApiClient));
            Guard.ArgumentNotNull(datasets, nameof(datasets));
            Guard.ArgumentNotNull(templateVersionChangedHandler, nameof(templateVersionChangedHandler));

            _jobManagement = jobManagement;
            _logger = logger;
            _datasets = datasets;
            _datasetsPolicy = resiliencePolicies.DatasetsApiClient;
            _templateVersionChangedHandler = templateVersionChangedHandler;
        }

        public async Task Run(
            SpecificationVersion specificationVersion, 
            SpecificationVersion previousSpecificationVersion,
            SpecificationEditModel editModel,
            Reference user, 
            string correlationId, 
            bool triggerProviderSnapshotDataLoadJob,
            bool triggerCalculationEngineRunJob)
        {
            string errorMessage = $"Unable to queue Edit Specification Job for specification - {specificationVersion.SpecificationId}";
            string triggerMessage = $"Assigning ProviderVersionId for specification: {specificationVersion.SpecificationId}";
            bool assignTemplateJobQueued = false;
            Job createEditSpecificationJob = null;

            try
            {
                createEditSpecificationJob = await CreateJob(errorMessage,
                    NewJobCreateModel(specificationVersion.SpecificationId,
                        triggerMessage,
                        JobConstants.DefinitionNames.EditSpecificationJob,
                        correlationId,
                        user,
                        new Dictionary<string, string>
                        {
                        { "specification-id", specificationVersion.SpecificationId },
                        { "user-id", user?.Id},
                        { "user-name", user?.Name}
                        }));

                GuardAgainstNullJob(createEditSpecificationJob, errorMessage);

                assignTemplateJobQueued = await _templateVersionChangedHandler.HandleTemplateVersionChanged(
                    previousSpecificationVersion,
                    specificationVersion,
                    editModel.AssignedTemplateIds,
                    user,
                    correlationId,
                    createEditSpecificationJob.Id);

                errorMessage = $"Unable to queue ProviderSnapshotDataLoadJob for specification - {specificationVersion.SpecificationId}";
                Reference fundingStream = specificationVersion.FundingStreams.FirstOrDefault();
                if (fundingStream != null &&
                    (specificationVersion.ProviderSource == Models.Providers.ProviderSource.FDZ ||
                    triggerProviderSnapshotDataLoadJob))
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
                        },
                        parentJobId: createEditSpecificationJob.Id)
                    );

                    GuardAgainstNullJob(createProviderSnapshotDataLoadJob, errorMessage);
                }
            }
            finally
            {
                if (createEditSpecificationJob != null && !assignTemplateJobQueued && !triggerProviderSnapshotDataLoadJob)
                {
                    await _jobManagement.UpdateJobStatus(createEditSpecificationJob.Id, 100, true, EditSpecificationJobOutCome);
                }
            }

            if (specificationVersion.ProviderSource == Models.Providers.ProviderSource.CFS && 
                !string.IsNullOrWhiteSpace(specificationVersion.ProviderVersionId))
            {
                ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>> response =
                        await _datasetsPolicy.ExecuteAsync(() => _datasets.GetRelationshipsBySpecificationId(specificationVersion.SpecificationId));

                DatasetSpecificationRelationshipViewModel providerRelationship = response?.Content?.Where(_ => _.IsProviderData).FirstOrDefault();

                if (providerRelationship != null && !providerRelationship.DatasetId.IsNullOrWhitespace())
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
                            { "isScopedJob", bool.TrueString },
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
