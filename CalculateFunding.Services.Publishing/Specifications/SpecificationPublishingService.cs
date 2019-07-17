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

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class SpecificationPublishingService : SpecificationPublishingBase, ISpecificationPublishingService
    {
        private readonly ICreateRefreshFundingJobs _jobs;

        public SpecificationPublishingService(IPublishSpecificationValidator validator,
            ISpecificationsApiClient specifications,
            IPublishingResiliencePolicies resiliencePolicies,
            ICreateRefreshFundingJobs jobs) : base(validator, specifications, resiliencePolicies)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));

            _jobs = jobs;
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

        private static bool AnySpecificationsInThisPeriodShareFundingStreams(
            IEnumerable<ApiSpecificationSummary> specificationsInFundingPeriod,
            IEnumerable<string> fundingStreams)
        {
            return specificationsInFundingPeriod.Any(_ => fundingStreams.Intersect(_.FundingStreams.Select(fs => fs.Id)).Any());
        }
    }
}