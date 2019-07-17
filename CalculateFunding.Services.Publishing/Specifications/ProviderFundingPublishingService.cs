using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class ProviderFundingPublishingService : SpecificationPublishingBase, IProviderFundingPublishingService
    {
        private readonly ICreatePublishFundingJobs _jobs;

        public ProviderFundingPublishingService(IPublishSpecificationValidator validator,
            ISpecificationsApiClient specifications,
            IPublishingResiliencePolicies resiliencePolicies,
            ICreatePublishFundingJobs jobs) : base(validator, specifications, resiliencePolicies)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));

            _jobs = jobs;
        }

        public async Task<IActionResult> PublishProviderFunding(string specificationId,
            Reference user,
            string correlationId)
        {
            ValidationResult validationResult = Validator.Validate(specificationId);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            ApiResponse<SpecificationSummary> specificationIdResponse =
                await ResiliencePolicy.ExecuteAsync(() => Specifications.GetSpecificationSummaryById(specificationId));

            SpecificationSummary specificationSummary = specificationIdResponse.Content;

            if (specificationSummary == null)
            {
                return new NotFoundResult();
            }

            if (!specificationSummary.IsSelectedForFunding)
            {
                return new PreconditionFailedResult("The Specification must be selected for funding");
            }

            Job publishProviderFundingJob = await _jobs.CreateJob(specificationId, user, correlationId);

            Guard.ArgumentNotNull(publishProviderFundingJob, nameof(publishProviderFundingJob),
                "Failed to create PublishProviderFundingJob");

            return new CreatedResult($"api/jobs/{publishProviderFundingJob.Id}", publishProviderFundingJob);
        }
    }
}