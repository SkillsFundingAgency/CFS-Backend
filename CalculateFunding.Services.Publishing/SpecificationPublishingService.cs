using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using PollyPolicy = Polly.Policy;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;
using ApiJob = CalculateFunding.Common.ApiClient.Jobs.Models.Job;

namespace CalculateFunding.Services.Publishing
{
    public class SpecificationPublishingService : ISpecificationPublishingService
    {
        private readonly IPublishSpecificationValidator _validator;
        private readonly ISpecificationsApiClient _specifications;
        private readonly ICreateRefreshFundingJobs _refreshFundingJobs;
        private readonly ICalcsResiliencePolicies _calcsResiliencePolicies;

        public SpecificationPublishingService(IPublishSpecificationValidator validator,
            ISpecificationsApiClient specifications,
            ICalcsResiliencePolicies calcsResiliencePolicies,
            ICreateRefreshFundingJobs refreshFundingJobs)
        {
            Guard.ArgumentNotNull(validator, nameof(validator));
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(calcsResiliencePolicies, nameof(calcsResiliencePolicies));
            Guard.ArgumentNotNull(refreshFundingJobs, nameof(refreshFundingJobs));

            _validator = validator;
            _specifications = specifications;
            _calcsResiliencePolicies = calcsResiliencePolicies;
            _refreshFundingJobs = refreshFundingJobs;
        }

        public async Task<IActionResult> CreatePublishJob(string specificationId,
            Reference user,
            string correlationId)
        {
            ValidationResult validationResult = _validator.Validate(specificationId);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            ApiResponse<ApiSpecificationSummary> specificationIdResponse =
                await ResiliencePolicy.ExecuteAsync(() => _specifications.GetSpecificationSummaryById(specificationId));

            ApiSpecificationSummary specificationSummary = specificationIdResponse.Content;

            if (specificationSummary == null)
            {
                return new NotFoundResult();
            }

            string fundingPeriodId = specificationSummary.FundingPeriod?.Id;

            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId),
                $"SpecificationSummary {specificationId} has no funding period id");

            ApiResponse<IEnumerable<ApiSpecificationSummary>> fundingPeriodIdResponse =
                await ResiliencePolicy.ExecuteAsync(() => _specifications.GetSpecificationsSelectedForFundingByPeriod(fundingPeriodId));

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

        private static bool AnySpecificationsInThisPeriodShareFundingStreams(
            IEnumerable<ApiSpecificationSummary> specificationsInFundingPeriod,
            IEnumerable<string> fundingStreams)
        {
            return specificationsInFundingPeriod.Any(_ => fundingStreams.Intersect(_.FundingStreams.Select(fs => fs.Id)).Any());
        }

        private PollyPolicy ResiliencePolicy => _calcsResiliencePolicies.SpecificationsRepositoryPolicy;
    }
}