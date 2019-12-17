using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using ApiJob = CalculateFunding.Common.ApiClient.Jobs.Models.Job;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class SpecificationPublishingService : SpecificationPublishingBase, ISpecificationPublishingService, IHealthChecker
    {
        private readonly ICreateRefreshFundingJobs _refreshFundingJobs;
        private readonly ICreateApproveFundingJobs _approveFundingJobs;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISpecificationFundingStatusService _specificationFundingStatusService;

        public SpecificationPublishingService(ISpecificationIdServiceRequestValidator validator,
            ISpecificationsApiClient specifications,
            IPublishingResiliencePolicies resiliencePolicies,
            ICacheProvider cacheProvider,
            ICreateRefreshFundingJobs refreshFundingJobs,
            ICreateApproveFundingJobs approveFundingJobs,
            ISpecificationFundingStatusService specificationFundingStatusService) : base(validator, specifications, resiliencePolicies)
        {
            Guard.ArgumentNotNull(refreshFundingJobs, nameof(refreshFundingJobs));
            Guard.ArgumentNotNull(approveFundingJobs, nameof(approveFundingJobs));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(specificationFundingStatusService, nameof(specificationFundingStatusService));

            _refreshFundingJobs = refreshFundingJobs;
            _cacheProvider = cacheProvider;
            _approveFundingJobs = approveFundingJobs;
            _specificationFundingStatusService = specificationFundingStatusService;
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

        public async Task<IActionResult> CreateRefreshFundingJob(string specificationId,
            Reference user,
            string correlationId)
        {
            ValidationResult validationResult = Validator.Validate(specificationId);

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

            ApiJob refreshFundingJob = await _refreshFundingJobs.CreateJob(specificationId, user, correlationId);

            Guard.ArgumentNotNull(refreshFundingJob, nameof(refreshFundingJob), "Failed to create RefreshFundingJob");

            JobCreationResponse jobCreationResponse = new JobCreationResponse()
            {
                JobId = refreshFundingJob.Id,
            };

            return new OkObjectResult(jobCreationResponse);
        }

        public async Task<IActionResult> ApproveSpecification(string action,
            string controller,
            string specificationId,
            Reference user,
            string correlationId)
        {
            ValidationResult validationResult = Validator.Validate(specificationId);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

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

            ApiJob job = await _approveFundingJobs.CreateJob(specificationId, user, correlationId);

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
                string errorMessage = $"Failed to create job of type '{JobConstants.DefinitionNames.ApproveFunding}' on specification '{specificationId}'";

                return new InternalServerErrorResult(errorMessage);
            }
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
    }
}