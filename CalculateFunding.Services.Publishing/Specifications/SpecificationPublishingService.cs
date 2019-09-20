using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ApiJob = CalculateFunding.Common.ApiClient.Jobs.Models.Job;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class SpecificationPublishingService : SpecificationPublishingBase, ISpecificationPublishingService, IHealthChecker
    {
        private readonly ICreateJobsForSpecifications<RefreshFundingJobDefinition> _refreshFundingJobs;
        private readonly ICreateJobsForSpecifications<ApproveFundingJobDefinition> _approveFundingJobs;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISpecificationFundingStatusService _specificationFundingStatusService;

        public SpecificationPublishingService(ISpecificationIdServiceRequestValidator validator,
            ISpecificationsApiClient specifications,
            IPublishingResiliencePolicies resiliencePolicies,
            ICacheProvider cacheProvider,
            ICreateJobsForSpecifications<RefreshFundingJobDefinition> refreshFundingJobs,
            ICreateJobsForSpecifications<ApproveFundingJobDefinition> approveFundingJobs,
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

            string fundingPeriodId = specificationSummary.FundingPeriod?.Id;

            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId),
                $"SpecificationSummary {specificationId} has no funding period id");

            ApiResponse<IEnumerable<ApiSpecificationSummary>> fundingPeriodIdResponse =
                await ResiliencePolicy.ExecuteAsync(() => Specifications.GetSpecificationsSelectedForFundingByPeriod(fundingPeriodId));

            IEnumerable<ApiSpecificationSummary> specificationsInFundingPeriod = fundingPeriodIdResponse.Content;

            if (AnySpecificationsInThisPeriodShareFundingStreams(specificationsInFundingPeriod,
                specificationSummary.FundingStreams.Select(_ => _.Id)))
            {
                return new ConflictResult();
            }

            ApiJob refreshFundingJob = await _refreshFundingJobs.CreateJob(specificationId, user, correlationId);

            Guard.ArgumentNotNull(refreshFundingJob, nameof(refreshFundingJob), "Failed to create RefreshFundingJob");

            return new CreatedResult($"api/jobs/{refreshFundingJob.Id}", refreshFundingJob);
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
                return new AcceptedAtActionResult(action, controller, new { specificationId = specificationId }, job);
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

        private static bool AnySpecificationsInThisPeriodShareFundingStreams(
            IEnumerable<ApiSpecificationSummary> specificationsInFundingPeriod,
            IEnumerable<string> fundingStreams)
        {
            if (specificationsInFundingPeriod.IsNullOrEmpty())
            {
                return false;
            }

            return specificationsInFundingPeriod.Any(_ => fundingStreams.Intersect(_.FundingStreams.Select(fs => fs.Id)).Any());
        }
    }
}