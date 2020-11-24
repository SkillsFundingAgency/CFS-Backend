using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
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
using ApiJob = CalculateFunding.Common.ApiClient.Jobs.Models.Job;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class SpecificationPublishingService : SpecificationPublishingBase, ISpecificationPublishingService, IHealthChecker
    {
        private readonly ICreateRefreshFundingJobs _refreshFundingJobs;
        private readonly ICreateApproveAllFundingJobs _approveSpecificationFundingJobs;
        private readonly ICreateApproveBatchFundingJobs _approveProviderFundingJobs;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISpecificationFundingStatusService _specificationFundingStatusService;
        private readonly IPrerequisiteCheckerLocator _prerequisiteCheckerLocator;
        private readonly IProviderService _providerService;

        public SpecificationPublishingService(
            ISpecificationIdServiceRequestValidator specificationIdValidator,
            IPublishedProviderIdsServiceRequestValidator publishedProviderIdsValidator,
            IProviderService providerService,
            ISpecificationsApiClient specifications,
            IPublishingResiliencePolicies resiliencePolicies,
            ICacheProvider cacheProvider,
            ICreateRefreshFundingJobs refreshFundingJobs,
            ICreateApproveAllFundingJobs approveSpecificationFundingJobs,
            ICreateApproveBatchFundingJobs approveProviderFundingJobs,
            ISpecificationFundingStatusService specificationFundingStatusService,
            IFundingConfigurationService fundingConfigurationService,
            IPrerequisiteCheckerLocator prerequisiteCheckerLocator)
            : base(specificationIdValidator, publishedProviderIdsValidator, specifications, resiliencePolicies, fundingConfigurationService)
        {
            Guard.ArgumentNotNull(refreshFundingJobs, nameof(refreshFundingJobs));
            Guard.ArgumentNotNull(approveSpecificationFundingJobs, nameof(approveSpecificationFundingJobs));
            Guard.ArgumentNotNull(approveProviderFundingJobs, nameof(approveProviderFundingJobs));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(specificationFundingStatusService, nameof(specificationFundingStatusService));
            Guard.ArgumentNotNull(prerequisiteCheckerLocator, nameof(prerequisiteCheckerLocator));
            Guard.ArgumentNotNull(providerService, nameof(providerService));

            _refreshFundingJobs = refreshFundingJobs;
            _cacheProvider = cacheProvider;
            _approveSpecificationFundingJobs = approveSpecificationFundingJobs;
            _approveProviderFundingJobs = approveProviderFundingJobs;
            _specificationFundingStatusService = specificationFundingStatusService;
            _prerequisiteCheckerLocator = prerequisiteCheckerLocator;
            _providerService = providerService;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) cacheRepoHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(SpecificationPublishingService)
            };

            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheRepoHealth.Ok, DependencyName = cacheRepoHealth.GetType().GetFriendlyName(), Message = cacheRepoHealth.Message });

            return health;
        }

        public async Task<IActionResult> ValidateSpecificationForRefresh(string specificationId)
        {
            List<string> prereqErrors = new List<string>();

            ValidationResult validationResult = SpecificationIdValidator.Validate(specificationId);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            ApiResponse<ApiSpecificationSummary> specificationIdResponse =
                await ResiliencePolicy.ExecuteAsync(() => Specifications.GetSpecificationSummaryById(specificationId));

            ApiSpecificationSummary specificationSummary = specificationIdResponse.Content;

            if (specificationSummary == null)
            {
                return new NotFoundResult();
            }

            IDictionary<string, Provider> scopedProviders = await _providerService.GetScopedProvidersForSpecification(specificationSummary.Id, specificationSummary.ProviderVersionId);

            IPrerequisiteChecker prerequisiteChecker = _prerequisiteCheckerLocator.GetPreReqChecker(PrerequisiteCheckerType.Refresh);
            try
            {
                await prerequisiteChecker.PerformChecks(
                        specificationSummary,
                        null,
                        Array.Empty<PublishedProvider>(),
                        scopedProviders?.Values);
            }
            catch (JobPrereqFailedException ex)
            {
                return new BadRequestObjectResult(ex.Errors.ToArray().ToModelStateDictionary());
            }
            
            return new NoContentResult();
        }

        public async Task<IActionResult> CreateRefreshFundingJob(string specificationId,
            Reference user,
            string correlationId)
        {
            ValidationResult validationResult = SpecificationIdValidator.Validate(specificationId);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            ApiResponse<ApiSpecificationSummary> specificationIdResponse =
                await ResiliencePolicy.ExecuteAsync(() => Specifications.GetSpecificationSummaryById(specificationId));

            ApiSpecificationSummary specificationSummary = specificationIdResponse.Content;

            if (specificationSummary == null)
            {
                return new NotFoundResult();
            }

            SpecificationFundingStatus chooseCheck = await _specificationFundingStatusService.CheckChooseForFundingStatus(specificationSummary);

            if (chooseCheck == SpecificationFundingStatus.SharesAlreadyChosenFundingStream)
            {
                return new ConflictResult();
            }

            IDictionary<string, Provider> scopedProviders = await _providerService.GetScopedProvidersForSpecification(specificationSummary.Id, specificationSummary.ProviderVersionId);

            // Check prerequisites for this specification to be chosen/refreshed
            IPrerequisiteChecker prerequisiteChecker = _prerequisiteCheckerLocator.GetPreReqChecker(PrerequisiteCheckerType.Refresh);
            try
            {
                await prerequisiteChecker.PerformChecks(
                        specificationSummary,
                        null,
                        Array.Empty<PublishedProvider>(),
                        scopedProviders?.Values);
            }
            catch (JobPrereqFailedException ex)
            {
                return new BadRequestObjectResult(new [] {$"Prerequisite check for refresh failed {ex.Message}"}.ToModelStateDictionary());
            }

            ApiJob refreshFundingJob = await _refreshFundingJobs.CreateJob(specificationId, user, correlationId);

            Guard.ArgumentNotNull(refreshFundingJob, nameof(refreshFundingJob), "Failed to create RefreshFundingJob");

            JobCreationResponse jobCreationResponse = new JobCreationResponse()
            {
                JobId = refreshFundingJob.Id,
            };

            return new OkObjectResult(jobCreationResponse);
        }

        public async Task<IActionResult> ApproveAllProviderFunding(
            string specificationId,
            Reference user,
            string correlationId)
        {
            ValidationResult validationResult = SpecificationIdValidator.Validate(specificationId);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            IActionResult actionResult = await IsReadyForApproval(specificationId, ApprovalMode.All);
            if (!actionResult.IsOk())
            {
                return actionResult;
            }

            ApiJob job = await _approveSpecificationFundingJobs.CreateJob(specificationId, user, correlationId);
            return ProcessJobResponse(job, specificationId, JobConstants.DefinitionNames.ApproveAllProviderFundingJob);
        }

        public async Task<IActionResult> ApproveBatchProviderFunding(string specificationId, PublishedProviderIdsRequest publishedProviderIdsRequest, Reference user, string correlationId)
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

            IActionResult actionResult = await IsReadyForApproval(specificationId, ApprovalMode.Batches);
            if (!actionResult.IsOk())
            {
                return actionResult;
            }

            Dictionary<string, string> messageProperties = new Dictionary<string, string>
            {
                { JobConstants.MessagePropertyNames.PublishedProviderIdsRequest, JsonExtensions.AsJson(publishedProviderIdsRequest) }
            };

            ApiJob job = await _approveProviderFundingJobs.CreateJob(specificationId, user, correlationId, messageProperties);
            return ProcessJobResponse(job, specificationId, JobConstants.DefinitionNames.ApproveBatchProviderFundingJob);
        }

        public async Task<IActionResult> CanChooseForFunding(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ApiResponse<ApiSpecificationSummary> specificationResponse =
               await ResiliencePolicy.ExecuteAsync(() => Specifications.GetSpecificationSummaryById(specificationId));

            ApiSpecificationSummary specificationSummary = specificationResponse?.Content;

            if (specificationSummary == null)
            {
                return new NotFoundResult();
            }

            SpecificationFundingStatus specificationFundingStatus = await _specificationFundingStatusService.CheckChooseForFundingStatus(specificationSummary);

            return new OkObjectResult(new SpecificationCheckChooseForFundingResult { Status = specificationFundingStatus });
        }

        private async Task<IActionResult> IsReadyForApproval(string specificationId, ApprovalMode expectedApprovalMode)
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

            if(fundingConfigurations.Values.Any(_ => _.ApprovalMode != expectedApprovalMode))
            {
                return new PreconditionFailedResult($"Specification with id : {specificationId} has funding configurations which does not match required approval mode={expectedApprovalMode}");
            }

            return new OkObjectResult(null);
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
    }
}