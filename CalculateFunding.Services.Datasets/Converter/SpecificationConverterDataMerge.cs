using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Processing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using static CalculateFunding.Services.Core.NonRetriableException;

namespace CalculateFunding.Services.Datasets.Converter
{
    public class SpecificationConverterDataMerge : JobProcessingService, ISpecificationConverterDataMerge
    {
        private const string RunConverterDatasetMergeJob = JobConstants.DefinitionNames.RunConverterDatasetMergeJob;
        private const string QueueConverterDatasetMergeJob = JobConstants.DefinitionNames.QueueConverterDatasetMergeJob;

        private readonly ISpecificationsApiClient _specifications;
        private readonly IPoliciesApiClient _policies;
        private readonly IDatasetRepository _datasets;
        private readonly AsyncPolicy _datasetsResilience;
        private readonly AsyncPolicy _policiesResilience;
        private readonly AsyncPolicy _specificationsResilience;
        private readonly IJobManagement _jobManagement;

        public SpecificationConverterDataMerge(ISpecificationsApiClient specifications,
            IPoliciesApiClient policies,
            IDatasetRepository datasets,
            IDatasetsResiliencePolicies resiliencePolicies,
            IJobManagement jobManagement,
            ILogger logger) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(policies, nameof(policies));
            Guard.ArgumentNotNull(datasets, nameof(datasets));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, "resiliencePolicies.SpecificationsApiClient");
            Guard.ArgumentNotNull(resiliencePolicies.PoliciesApiClient, "resiliencePolicies.PoliciesApiClient");
            Guard.ArgumentNotNull(resiliencePolicies.DatasetRepository, "resiliencePolicies.DatasetRepository");

            _specifications = specifications;
            _policies = policies;
            _datasets = datasets;
            _jobManagement = jobManagement;
            _specificationsResilience = resiliencePolicies.SpecificationsApiClient;
            _policiesResilience = resiliencePolicies.PoliciesApiClient;
            _datasetsResilience = resiliencePolicies.DatasetRepository;
        }

        public async Task<IActionResult> QueueJob(SpecificationConverterMergeRequest request)
        {
            Guard.ArgumentNotNull(request, nameof(request));

            string specificationId = request.SpecificationId;

            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            Reference author = request.Author;

            Job job = await QueueJob(new JobCreateModel
            {
                JobDefinitionId = QueueConverterDatasetMergeJob,
                SpecificationId = specificationId,
                MessageBody = request.AsJson(),
                InvokerUserId = author?.Id,
                InvokerUserDisplayName = author?.Name,
                Properties = new Dictionary<string, string>
                {
                    {"specification-id", specificationId}
                },
                Trigger = new Trigger
                {
                    EntityId = specificationId,
                    EntityType = "Specification"
                }
            });
            
            return new OkObjectResult(new JobCreationResponse
            {
                JobId = job.Id
            });
        }

        public override async Task Process(Message message)
        {
            EnsureIsNotNull(message, "No message to process");

            AutoComplete = false;
            
            Guard.ArgumentNotNull(message, nameof(message));

            SpecificationConverterMergeRequest specificationConverterMergeRequest = message.GetPayloadAsInstanceOf<SpecificationConverterMergeRequest>();
            
            EnsureIsNotNull(specificationConverterMergeRequest, "Message does not contain a SpecificationConverterMergeRequest to process");

            Guard.ArgumentNotNull(specificationConverterMergeRequest, nameof(specificationConverterMergeRequest));

            string specificationId = specificationConverterMergeRequest.SpecificationId;

            SpecificationSummary specification = await GetSpecification(specificationId);

            if (await ConverterWizardIsNotEnabled(specification))
            {
                AutoComplete = true;

                Information($"Funding configuration for specification {specificationId} does not have the converter wizard enabled.");

                return;
            }

            IEnumerable<DefinitionSpecificationRelationship> specificationDatasetRelationships = await GetDatasetRelationships(specificationId);

            if (!await QueueJobForSpecificationDatasets(specificationDatasetRelationships,
                specificationConverterMergeRequest.Author,
                specification.ProviderVersionId))
            {
                // if no children are queued then make sure that we auto complete the job
                AutoComplete = true;
            }
        }

        private async Task<bool> QueueJobForSpecificationDatasets(IEnumerable<DefinitionSpecificationRelationship> specificationDatasetRelationships,
            Reference author,
            string providerVersionId)
        {
            string parentJobId = Job.Id;
            bool jobQueued = false;

            foreach (DefinitionSpecificationRelationship specificationDatasetRelationship in specificationDatasetRelationships)
            {
                if (!specificationDatasetRelationship.Current.ConverterEnabled)
                {
                    continue;
                }

                jobQueued = true;

                string datasetRelationshipId = specificationDatasetRelationship.Id;
                
                await QueueJob(new JobCreateModel
                {
                    JobDefinitionId = RunConverterDatasetMergeJob,
                    ParentJobId = parentJobId,
                    Properties = new Dictionary<string, string>
                    {
                        {"dataset-relationship-id", datasetRelationshipId}
                    },
                    Trigger = new Trigger
                    {
                        EntityId = specificationDatasetRelationship.Current.Specification.Id,
                        EntityType = "Specification",
                    },
                    MessageBody = new ConverterMergeRequest
                    {
                        DatasetRelationshipId = datasetRelationshipId,
                        DatasetId = specificationDatasetRelationship.Current.DatasetVersion.Id,
                        Version = specificationDatasetRelationship.Current.DatasetVersion.Version.ToString(),
                        ProviderVersionId = providerVersionId,
                        Author = author
                    }.AsJson()
                });
            }

            return jobQueued;
        }

        private async Task<IEnumerable<DefinitionSpecificationRelationship>> GetDatasetRelationships(string specificationId) =>
            await _datasetsResilience.ExecuteAsync(() => _datasets.GetDefinitionSpecificationRelationshipsByQuery(_ =>
                _.Content.Current.Specification.Id == specificationId));

        private async Task<SpecificationSummary> GetSpecification(string specificationId)
        {
            ApiResponse<SpecificationSummary> specificationSummaryResponse = await _specificationsResilience.ExecuteAsync(()
                => _specifications.GetSpecificationSummaryById(specificationId));

            SpecificationSummary specificationSummary = specificationSummaryResponse?.Content;

            EnsureIsNotNull(specificationSummary, $"Did not locate specification with Id {specificationId}");

            return specificationSummary;
        }

        private async Task<bool> ConverterWizardIsNotEnabled(SpecificationSummary specification) 
            => !(await GetFundingConfiguration(specification)).EnableConverterDataMerge;

        private async Task<FundingConfiguration> GetFundingConfiguration(SpecificationSummary specification)
        {
            string fundingPeriodId = specification.FundingPeriod.Id;
            string fundingStreamId = specification.FundingStreams.Single().Id;

            ApiResponse<FundingConfiguration> fundingConfigurationResponse = await _policiesResilience.ExecuteAsync(()
                => _policies.GetFundingConfiguration(fundingStreamId,
                    fundingPeriodId));

            FundingConfiguration fundingConfiguration = fundingConfigurationResponse?.Content;

            EnsureIsNotNull(fundingConfiguration, $"Did not locate a funding configuration for {fundingStreamId} {fundingPeriodId}");

            return fundingConfiguration;
        }
    }
}