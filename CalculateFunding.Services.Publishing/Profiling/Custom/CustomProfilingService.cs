using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
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
        private readonly ILogger _logger;

        public CustomProfilingService(IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IValidator<ApplyCustomProfileRequest> requestValidation,
            IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(requestValidation, nameof(requestValidation));
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            
            _publishedProviderVersionCreation = publishedProviderStatusUpdateService;
            _requestValidation = requestValidation;
            _publishedFundingRepository = publishedFundingRepository;
            _logger = logger;
            _publishedFundingResilience = resiliencePolicies.PublishedFundingRepository;
        }

        public async Task<IActionResult> ApplyCustomProfile(ApplyCustomProfileRequest request, Reference author)
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
            
            PublishedProvider publishedProvider = await _publishedFundingResilience.ExecuteAsync(() => 
                _publishedFundingRepository.GetPublishedProviderById(publishedProviderId, publishedProviderId));

            PublishedProviderVersion current = publishedProvider.Current;

            current.CustomProfiles = request.ProfileOverrides.DeepCopy();
            
            Dictionary<string, FundingLine> fundingLines = current.FundingLines.ToDictionary(_ => _.FundingLineCode);

            foreach (FundingLineProfileOverrides profileOverride in request.ProfileOverrides)
            {
                FundingLine fundingLine = fundingLines[profileOverride.FundingLineCode];

                fundingLine.DistributionPeriods = profileOverride.DistributionPeriods.DeepCopy();

                if (profileOverride.HasCarryOver)
                {
                    current.AddCarryOver(fundingLine.FundingLineCode,
                        ProfilingCarryOverType.CustomProfile,
                        profileOverride.CarryOver.GetValueOrDefault());
                }

                current.AddProfilingAudit(profileOverride.FundingLineCode, author);
            }

            await _publishedProviderVersionCreation.UpdatePublishedProviderStatus(new[] {publishedProvider}, 
                author, 
                current.Status switch
                {
                    PublishedProviderStatus.Draft => PublishedProviderStatus.Draft,
                    _ => PublishedProviderStatus.Updated
                });
            
            _logger.Information(
                $"Successfully applied custom profiling {request.CustomProfileName} to published provider {publishedProviderId}");
            
            return new NoContentResult();
        }
    }
}