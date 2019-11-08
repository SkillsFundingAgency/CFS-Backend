using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class ProviderFundingPublishingService : SpecificationPublishingBase, IProviderFundingPublishingService
    {
        private readonly ICreateJobsForSpecifications<PublishProviderFundingJobDefinition> _jobs;
        private readonly IPublishedFundingRepository _publishedFundingRepository;

        public ProviderFundingPublishingService(ISpecificationIdServiceRequestValidator validator,
            ISpecificationsApiClient specifications,
            IPublishingResiliencePolicies resiliencePolicies,
            ICreateJobsForSpecifications<PublishProviderFundingJobDefinition> jobs,
            IPublishedFundingRepository publishedFundingRepository) : base(validator, specifications,
            resiliencePolicies)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));

            _jobs = jobs;
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

        public async Task<IActionResult> PublishProviderFunding(string specificationId,
            Reference user,
            string correlationId)
        {
            ValidationResult validationResult = Validator.Validate(specificationId);

            if (!validationResult.IsValid) return validationResult.AsBadRequest();

            var specificationIdResponse =
                await ResiliencePolicy.ExecuteAsync(() => Specifications.GetSpecificationSummaryById(specificationId));

            SpecificationSummary specificationSummary = specificationIdResponse.Content;

            if (specificationSummary == null) return new NotFoundResult();

            if (!specificationSummary.IsSelectedForFunding)
                return new PreconditionFailedResult("The Specification must be selected for funding");

            Job publishProviderFundingJob = await _jobs.CreateJob(specificationId, user, correlationId);

            Guard.ArgumentNotNull(publishProviderFundingJob, nameof(publishProviderFundingJob),
                "Failed to create PublishProviderFundingJob");

            JobCreationResponse jobCreationResponse = new JobCreationResponse()
            {
                JobId = publishProviderFundingJob.Id,
            };

            return new OkObjectResult(jobCreationResponse);
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
    }
}