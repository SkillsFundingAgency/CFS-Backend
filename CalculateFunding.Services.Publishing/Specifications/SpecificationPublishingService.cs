using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;
using ApiJob = CalculateFunding.Common.ApiClient.Jobs.Models.Job;
using CalculateFunding.Models.Specs;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Common.Models.HealthCheck;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class SpecificationPublishingService : SpecificationPublishingBase, ISpecificationPublishingService, IHealthChecker
    {
        private readonly ICreateJobsForSpecifications<RefreshFundingJobDefinition> _jobs;
        private readonly ICreateJobsForSpecifications<ApproveFundingJobDefinition> _approveFundingJobs;
        private readonly ICacheProvider _cacheProvider;

        public SpecificationPublishingService(IPublishSpecificationValidator validator,
            ISpecificationsApiClient specifications,
            IPublishingResiliencePolicies resiliencePolicies,
            ICacheProvider cacheProvider,
            ICreateJobsForSpecifications<RefreshFundingJobDefinition> jobs,
            ICreateJobsForSpecifications<ApproveFundingJobDefinition> approveFundingJobs) : base(validator, specifications, resiliencePolicies)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(approveFundingJobs, nameof(approveFundingJobs));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));

            _jobs = jobs;
            _cacheProvider = cacheProvider;
            _approveFundingJobs = approveFundingJobs;
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

        public async Task<IActionResult> CreatePublishJob(string specificationId,
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

            ApiJob refreshFundingJob = await _jobs.CreateJob(specificationId, user, correlationId);

            Guard.ArgumentNotNull(refreshFundingJob, nameof(refreshFundingJob), "Failed to create RefreshFundingJob");

            return new CreatedResult($"api/jobs/{refreshFundingJob.Id}", refreshFundingJob);
        }

        public async Task<IActionResult> ApproveSpecification(string action,
            string controller,
            string specificationId,
            HttpRequest request,
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

            if(specificationSummary == null)
            {
                return new NotFoundResult();
            }

            string json = await request.GetRawBodyStringAsync();

            SpecificationApprovalModel approvalModel = JsonConvert.DeserializeObject<SpecificationApprovalModel>(json);

            Guard.IsNullOrWhiteSpace(approvalModel.FundingStreamId, nameof(approvalModel.FundingStreamId));

            if (!specificationSummary.IsSelectedForFunding)
            {
                return new PreconditionFailedResult($"Specification with id : {specificationId} has not been selected for funding");
            }

            string cacheKey = $"{CacheKeys.ApproveFundingForSpecification}{specificationId}:{Guid.NewGuid()}";

            await _cacheProvider.CreateListAsync<string>(approvalModel.Providers, cacheKey);

            Dictionary<string, string> properties = new Dictionary<string, string> { { "fundingStreamId", approvalModel.FundingStreamId }, { "cacheKey", cacheKey } };

            ApiJob job = await _approveFundingJobs.CreateJob(specificationId, user, correlationId, properties, json);

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

        private static bool AnySpecificationsInThisPeriodShareFundingStreams(
            IEnumerable<ApiSpecificationSummary> specificationsInFundingPeriod,
            IEnumerable<string> fundingStreams)
        {
            return specificationsInFundingPeriod.Any(_ => fundingStreams.Intersect(_.FundingStreams.Select(fs => fs.Id)).Any());
        }
    }
}