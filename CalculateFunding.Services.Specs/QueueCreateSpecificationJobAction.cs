using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Specs.Interfaces;
using Polly;
using Serilog;
using Calculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Specs
{
    public class QueueCreateSpecificationJobAction : IQueueCreateSpecificationJobActions
    {
        private readonly IPoliciesApiClient _policies;
        private readonly IJobManagement _jobManagement;
        private readonly AsyncPolicy _policyResiliencePolicy;
        private readonly ILogger _logger;

        public QueueCreateSpecificationJobAction(IPoliciesApiClient policies,
            IJobManagement jobManagement,
            ISpecificationsResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(policies, nameof(policies));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _policies = policies;
            _jobManagement = jobManagement;
            _logger = logger;
            _policyResiliencePolicy = resiliencePolicies.PoliciesApiClient;
        }

        public async Task Run(SpecificationVersion specificationVersion,
            Reference user,
            string correlationId)
        {
            string versionSpecificationId = specificationVersion.SpecificationId;

            string errorMessage = $"Unable to queue create specification job for {versionSpecificationId}";

            Job createSpecificationJob = await CreateJob(errorMessage,
                NewJobCreateModel(versionSpecificationId,
                    "Creating Specification",
                    JobConstants.DefinitionNames.CreateSpecificationJob,
                    correlationId,
                    user,
                    new Dictionary<string, string>
                    {
                        {"specification-id", versionSpecificationId}
                    }));

            GuardAgainstNullJob(createSpecificationJob, errorMessage);

            string fundingPeriodId = specificationVersion.FundingPeriod.Id;

            Task[] createAssignTemplateJobTasks = specificationVersion.FundingStreams.Select(
                _ => CreateAssignCalculationJobForFundingStream(_.Id,
                    fundingPeriodId,
                    specificationVersion,
                    user,
                    correlationId,
                    createSpecificationJob.Id)).ToArray();

            await TaskHelper.WhenAllAndThrow(createAssignTemplateJobTasks);

            Reference fundingStream = specificationVersion.FundingStreams.FirstOrDefault();
            if(fundingStream != null && specificationVersion.ProviderSource == Models.Providers.ProviderSource.FDZ)
            {
                errorMessage = $"Unable to queue ProviderSnapshotDataLoadJob for specification - {versionSpecificationId}";
                Job createProviderSnapshotDataLoadJob = await CreateJob(errorMessage,
                NewJobCreateModel(versionSpecificationId,
                    "Assigning ProviderVersionId for specification",
                    JobConstants.DefinitionNames.ProviderSnapshotDataLoadJob,
                    correlationId,
                    user,
                    new Dictionary<string, string>
                    {
                        {"specification-id", versionSpecificationId},
                        {"fundingstream-id", fundingStream.Id},
                        {"providerSanpshot-id", specificationVersion.ProviderSnapshotId?.ToString() }
                    }));

                GuardAgainstNullJob(createProviderSnapshotDataLoadJob, errorMessage);
            }
        }

        private async Task CreateAssignCalculationJobForFundingStream(string fundingStreamId,
            string fundingPeriodId,
            SpecificationVersion specificationVersion,
            Reference user,
            string correlationId,
            string parentJobId)
        { 
            string templateVersion = specificationVersion.TemplateIds.ContainsKey(fundingStreamId) ? specificationVersion.TemplateIds[fundingStreamId] : string.Empty;

            if (string.IsNullOrEmpty(templateVersion)) return;

            ApiResponse<TemplateMetadataContents> templateContents = await _policyResiliencePolicy.ExecuteAsync(
                () => _policies.GetFundingTemplateContents(fundingStreamId, fundingPeriodId,
                    templateVersion));

            IEnumerable<FundingLine> flattenedFundingLines = templateContents?.Content?.RootFundingLines.Flatten(_ => _.FundingLines)
                                                             ?? new FundingLine[0];

            IEnumerable<Calculation> flattenedCalculations = flattenedFundingLines.SelectMany(_ => _.Calculations.Flatten(cal => cal.Calculations)) ?? new Calculation[0];

            IEnumerable<Calculation> uniqueflattenedCalculations = flattenedCalculations.GroupBy(x => x.TemplateCalculationId).Select(x => x.FirstOrDefault());

            int itemCount = uniqueflattenedCalculations?.Count() + uniqueflattenedCalculations?.Sum(
                       cal => (cal.ReferenceData?.Count())
                           .GetValueOrDefault()) ?? 0;

            if (itemCount == 0)
            {
                _logger.Warning("Did not locate any calculations on to queue assignment job for");

                return;
            }

            Job assignCalculationsJob = await CreateJob($"Failed to queue assign template calculations job for {fundingStreamId}/{templateVersion}",
                NewJobCreateModel(specificationVersion.SpecificationId,
                    "Assigning Template Calculations for Specification",
                    JobConstants.DefinitionNames.AssignTemplateCalculationsJob,
                    correlationId,
                    user,
                    new Dictionary<string, string>
                    {
                        {"specification-id", specificationVersion.SpecificationId},
                        {"fundingstream-id", fundingStreamId},
                        {"fundingperiod-id", fundingPeriodId},
                        {"template-version", templateVersion}
                    },
                    parentJobId,
                    itemCount));

            GuardAgainstNullJob(assignCalculationsJob, $"Failed to queue assign template calculations job for {fundingStreamId}/{templateVersion}");
        }

        private JobCreateModel NewJobCreateModel(string specificationId,
            string message,
            string jobDefinitionId,
            string correlationId,
            Reference user,
            IDictionary<string, string> properties,
            string parentJobId = null,
            int? itemCount = null)
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
                ItemCount = itemCount
            };
        }

        private async Task<Job> CreateJob(string errorMessage, JobCreateModel createModel)
        {
            try
            {
                Job job = await _jobManagement.QueueJob(createModel);

                return job;
            }
            catch(Exception ex)
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