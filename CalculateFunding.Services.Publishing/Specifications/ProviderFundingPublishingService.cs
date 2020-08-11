using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;
using ApiJob = CalculateFunding.Common.ApiClient.Jobs.Models.Job;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class ProviderFundingPublishingService : SpecificationPublishingBase, IProviderFundingPublishingService, IHealthChecker
    {
        private readonly ICreateAllPublishProviderFundingJobs _createAllPublishProviderFundingJobs;
        private readonly ICreateBatchPublishProviderFundingJobs _createBatchPublishProviderFundingJobs;
        private readonly IPublishedFundingRepository _publishedFundingRepository;

        public ProviderFundingPublishingService(
            ISpecificationIdServiceRequestValidator specificationIdValidator,
            IPublishedProviderIdsServiceRequestValidator publishedProviderIdsValidator,
            ISpecificationsApiClient specifications,
            IPublishingResiliencePolicies resiliencePolicies,
            ICreateAllPublishProviderFundingJobs createAllPublishProviderFundingJobs,
            ICreateBatchPublishProviderFundingJobs createBatchPublishProviderFundingJobs,
            IPublishedFundingRepository publishedFundingRepository,
            IFundingConfigurationService fundingConfigurationService) : 
            base(specificationIdValidator, publishedProviderIdsValidator, specifications,resiliencePolicies, fundingConfigurationService)
        {
            Guard.ArgumentNotNull(createAllPublishProviderFundingJobs, nameof(createAllPublishProviderFundingJobs));
            Guard.ArgumentNotNull(createBatchPublishProviderFundingJobs, nameof(createBatchPublishProviderFundingJobs));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));

            _createAllPublishProviderFundingJobs = createAllPublishProviderFundingJobs;
            _createBatchPublishProviderFundingJobs = createBatchPublishProviderFundingJobs;
            _publishedFundingRepository = publishedFundingRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(ProviderFundingPublishingService)
            };

            health.Dependencies.AddRange((await _publishedFundingRepository.IsHealthOk()).Dependencies);

            return health;
        }

        public async Task<IActionResult> PublishAllProvidersFunding(string specificationId,
            Reference user,
            string correlationId)
        {
            ValidationResult validationResult = SpecificationIdValidator.Validate(specificationId);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            IActionResult actionResult = await IsSpecificationReadyForPublish(specificationId, ApprovalMode.All);
            if (!actionResult.IsOk())
            {
                return actionResult;
            }

            ApiJob job = await _createAllPublishProviderFundingJobs.CreateJob(specificationId, user, correlationId);
            return ProcessJobResponse(job, specificationId, JobConstants.DefinitionNames.PublishAllProviderFundingJob);
        }

        public async Task<IActionResult> PublishBatchProvidersFunding(string specificationId,
            PublishedProviderIdsRequest publishedProviderIdsRequest,
            Reference user,
            string correlationId)
        {
            ValidationResult specificationIdValidationResult = SpecificationIdValidator.Validate(specificationId);
            if (!specificationIdValidationResult.IsValid)
            {
                return specificationIdValidationResult.AsBadRequest();
            }

            ValidationResult publishedProviderIdsValidationResult = PublishedProviderIdsValidator.Validate(publishedProviderIdsRequest.PublishedProviderIds.ToArray());
            if (!publishedProviderIdsValidationResult.IsValid)
            {
                return publishedProviderIdsValidationResult.AsBadRequest();
            }

            IActionResult actionResult = await IsSpecificationReadyForPublish(specificationId, ApprovalMode.Batches);
            if (!actionResult.IsOk())
            {
                return actionResult;
            }

            Dictionary<string, string> messageProperties = new Dictionary<string, string>
            {
                { JobConstants.MessagePropertyNames.PublishedProviderIdsRequest, JsonExtensions.AsJson(publishedProviderIdsRequest) }
            };

            ApiJob job = await _createBatchPublishProviderFundingJobs.CreateJob(specificationId, user, correlationId, messageProperties);
            return ProcessJobResponse(job, specificationId, JobConstants.DefinitionNames.PublishBatchProviderFundingJob);
        }

        public async Task<IActionResult> GetPublishedProviderTransactions(string specificationId,
            string providerId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            IEnumerable<PublishedProviderVersion> providerVersions = await ResiliencePolicy.ExecuteAsync(() =>
                _publishedFundingRepository.GetPublishedProviderVersions(specificationId,
                    providerId));

            return new OkObjectResult(providerVersions?.Select(_ => new PublishedProviderTransaction
            {
                PublishedProviderId = _.PublishedProviderId,
                Author = _.Author,
                Date = _.Date,
                Status = _.Status,
                TotalFunding = _.TotalFunding,
                FundingLines = _.FundingLines
            }));
        }

        public async Task<IActionResult> GetPublishedProviderVersion(string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            string version)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));
            Guard.IsNullOrWhiteSpace(version, nameof(version));

            PublishedProviderVersion providerVersion = await ResiliencePolicy.ExecuteAsync(() =>
                _publishedFundingRepository.GetPublishedProviderVersion(fundingStreamId,
                    fundingPeriodId,
                    providerId,
                    version));

            if (providerVersion == null) return new NotFoundResult();

            return new OkObjectResult(providerVersion);
        }

        private IActionResult ProcessJobResponse(ApiJob job, string specificationId, string jobType)
        {
            if (job != null)
            {
                JobCreationResponse jobCreationResponse = new JobCreationResponse()
                {
                    JobId = job.Id,
                };

                return new OkObjectResult(jobCreationResponse);
            }
            else
            {
                string errorMessage = $"Failed to create job of type '{jobType}' on specification '{specificationId}'";

                return new InternalServerErrorResult(errorMessage);
            }
        }

        private async Task<IActionResult> IsSpecificationReadyForPublish(string specificationId, ApprovalMode expectedApprovalMode)
        {
            ApiResponse<ApiSpecificationSummary> specificationIdResponse = await Specifications.GetSpecificationSummaryById(specificationId);
            ApiSpecificationSummary specificationSummary = specificationIdResponse.Content;

            if (specificationSummary == null)
            {
                return new NotFoundResult();
            }

            if (!specificationSummary.IsSelectedForFunding)
            {
                return new PreconditionFailedResult($"Specification with id : {specificationId} has not been selected for funding");
            }

            IDictionary<string, FundingConfiguration> fundingConfigurations = await _fundingConfigurationService.GetFundingConfigurations(specificationSummary);

            if (fundingConfigurations.Values.Any(_ => _.ApprovalMode != expectedApprovalMode))
            {
                return new PreconditionFailedResult($"Specification with id : {specificationId} has funding configurations which does not match required approval mode={expectedApprovalMode}");
            }

            return new OkObjectResult(null);
        }
    }
}