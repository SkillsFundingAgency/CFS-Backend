using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.Profiling.Custom
{
    public class CustomProfilingService : ICustomProfileService
    {
        private readonly AsyncPolicy _publishedFundingResilience;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly IPublishedProviderStatusUpdateService _publishedProviderVersionCreation;
        private readonly IValidator<ApplyCustomProfileRequest> _requestValidation;
        private readonly IPublishedFundingCsvJobsService _publishFundingCsvJobsService;
        private readonly ILogger _logger;

        public CustomProfilingService(IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IValidator<ApplyCustomProfileRequest> requestValidation,
            IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies resiliencePolicies,
            IPublishedFundingCsvJobsService publishFundingCsvJobsService,
            ILogger logger)
        {
            Guard.ArgumentNotNull(requestValidation, nameof(requestValidation));
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishFundingCsvJobsService, nameof(publishFundingCsvJobsService));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _publishedProviderVersionCreation = publishedProviderStatusUpdateService;
            _requestValidation = requestValidation;
            _publishedFundingRepository = publishedFundingRepository;
            _logger = logger;
            _publishedFundingResilience = resiliencePolicies.PublishedFundingRepository;
            _publishFundingCsvJobsService = publishFundingCsvJobsService;
        }

        public async Task<IActionResult> ApplyCustomProfile(ApplyCustomProfileRequest request, Reference author, string correlationId)
        {
            Guard.ArgumentNotNull(request, nameof(request));
            Guard.ArgumentNotNull(author, nameof(author));

            ValidationResult validationResult = await _requestValidation.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                string validationErrors = validationResult.Errors.Select(_ => _.ErrorMessage).Join(", ");

                _logger.Information(
                    $"Unable to process apply custom profile request. Request was invalid. \n{validationErrors}");

                return validationResult.AsBadRequest();
            }

            string publishedProviderId = request.PublishedProviderId;
            string fundingLineCode = request.FundingLineCode;

            PublishedProvider publishedProvider = await _publishedFundingResilience.ExecuteAsync(() =>
                _publishedFundingRepository.GetPublishedProviderById(publishedProviderId, publishedProviderId));

            PublishedProviderVersion currentProviderVersion = publishedProvider.Current;

            currentProviderVersion.VerifyProfileAmountsMatchFundingLineValue(fundingLineCode, request.ProfilePeriods, request.CarryOver);

            foreach (IGrouping<string, ProfilePeriod> profilePeriods in request.ProfilePeriods.GroupBy(_ => _.DistributionPeriodId))
            {
                string distributionPeriodId = profilePeriods.Key;

                currentProviderVersion.UpdateDistributionPeriodForFundingLine(
                    fundingLineCode,
                    distributionPeriodId,
                    profilePeriods);

                currentProviderVersion.AddOrUpdateCustomProfile(fundingLineCode, request.CarryOver, distributionPeriodId);
            }

            if (request.HasCarryOver)
            {
                currentProviderVersion.AddCarryOver(fundingLineCode,
                    ProfilingCarryOverType.CustomProfile,
                    request.CarryOver.GetValueOrDefault());
            }
            else
            {
                currentProviderVersion.RemoveCarryOver(fundingLineCode);
            }

            currentProviderVersion.AddProfilingAudit(fundingLineCode, author);

            await _publishedProviderVersionCreation.UpdatePublishedProviderStatus(new[] { publishedProvider },
                author,
                currentProviderVersion.Status switch
                {
                    PublishedProviderStatus.Draft => PublishedProviderStatus.Draft,
                    _ => PublishedProviderStatus.Updated
                },
                force: true);

            _logger.Information(
                $"Successfully applied custom profiling {request.CustomProfileName} to published provider {publishedProviderId}");

            await _publishFundingCsvJobsService.QueueCsvJobs(GeneratePublishingCsvJobsCreationAction.Refresh,
                currentProviderVersion.SpecificationId,
                correlationId,
                author);

            return new NoContentResult();
        }
    }
}