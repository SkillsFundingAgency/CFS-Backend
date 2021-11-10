using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderProfilingService : IPublishedProviderProfilingService
    {
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly ILogger _logger;
        private readonly IPoliciesService _policiesService;
        private readonly AsyncPolicy _publishingResiliencePolicy;
        private readonly AsyncPolicy _specificationResiliencePolicy;
        private readonly AsyncPolicy _profilingPolicy;
        private readonly IProfilingApiClient _profiling;
        private readonly IPublishedProviderErrorDetection _publishedProviderErrorDetection;
        private readonly IProfilingService _profilingService;
        private readonly IPublishedProviderVersioningService _publishedProviderVersioningService;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IReProfilingRequestBuilder _profilingRequestBuilder;
        private readonly IReProfilingResponseMapper _reProfilingResponseMapper;

        public PublishedProviderProfilingService(IPublishedFundingRepository publishedFundingRepository,
            IPublishedProviderErrorDetection publishedProviderErrorDetection,
            IProfilingService profilingService,
            IPublishedProviderVersioningService publishedProviderVersioningService,
            ISpecificationsApiClient specificationsApiClient,
            IReProfilingRequestBuilder profilingRequestBuilder,
            IProfilingApiClient profiling,
            IPoliciesService policiesService,
            IReProfilingResponseMapper reProfilingResponseMapper,
            ILogger logger,
            IPublishingResiliencePolicies publishingResiliencePolicies)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishedProviderErrorDetection, nameof(publishedProviderErrorDetection));
            Guard.ArgumentNotNull(profilingService, nameof(profilingService));
            Guard.ArgumentNotNull(publishedProviderVersioningService, nameof(publishedProviderVersioningService));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(profilingRequestBuilder, nameof(profilingRequestBuilder));
            Guard.ArgumentNotNull(profiling, nameof(profiling));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.SpecificationsApiClient, nameof(publishingResiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.ProfilingApiClient, nameof(publishingResiliencePolicies.ProfilingApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(reProfilingResponseMapper, nameof(reProfilingResponseMapper));

            _publishedFundingRepository = publishedFundingRepository;
            _publishedProviderErrorDetection = publishedProviderErrorDetection;
            _profilingService = profilingService;
            _publishedProviderVersioningService = publishedProviderVersioningService;
            _specificationsApiClient = specificationsApiClient;
            _profilingRequestBuilder = profilingRequestBuilder;
            _profiling = profiling;
            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _specificationResiliencePolicy = publishingResiliencePolicies.SpecificationsApiClient;
            _profilingPolicy = publishingResiliencePolicies.ProfilingApiClient;
            _logger = logger;
            _policiesService = policiesService;
            _reProfilingResponseMapper = reProfilingResponseMapper;
        }

        public async Task<IActionResult> AssignProfilePatternKey(
            string fundingStreamId, 
            string fundingPeriodId, 
            string providerId, 
            ProfilePatternKey profilePatternKey,
            Reference author)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));
            Guard.ArgumentNotNull(profilePatternKey, nameof(profilePatternKey));

            PublishedProvider publishedProvider = await _publishingResiliencePolicy.ExecuteAsync(async () => 
                await _publishedFundingRepository.GetPublishedProvider(fundingStreamId, fundingPeriodId, providerId));

            if (publishedProvider == null)
            {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            FundingConfiguration fundingConfiguration = await _policiesService.GetFundingConfiguration(fundingStreamId, fundingPeriodId);
            if(fundingConfiguration == null || !fundingConfiguration.EnableUserEditableRuleBasedProfiles)
            {
                return new BadRequestObjectResult($"User not allowed to edit rule based profiles for funding stream - '{fundingStreamId}' and funding period - '{fundingPeriodId}'");
            }

            if (MatchingProfilePatternKeyExists(publishedProvider.Current, profilePatternKey))
            {
                return new StatusCodeResult((int)HttpStatusCode.NotModified);
            }

            PublishedProvider modifiedPublishedProvider = await CreateVersion(publishedProvider, author);

            if (modifiedPublishedProvider == null)
            {
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            PublishedProviderVersion newPublishedProviderVersion = publishedProvider.Current;

            newPublishedProviderVersion.SetProfilePatternKey(profilePatternKey, author);

            // remove any custom profiles for funding line
            newPublishedProviderVersion.CustomProfiles = newPublishedProviderVersion.CustomProfiles?.Where(_ => _.FundingLineCode != profilePatternKey.FundingLineCode);

            await ProfileFundingLineValues(newPublishedProviderVersion, profilePatternKey);

            PublishedProvidersContext context = new PublishedProvidersContext { FundingConfiguration = fundingConfiguration };

            await _publishedProviderErrorDetection.ApplyAssignProfilePatternErrorDetection(publishedProvider, context);

            await SavePublishedProvider(publishedProvider, newPublishedProviderVersion);

            return new StatusCodeResult((int)HttpStatusCode.OK);
        }

        private async Task<PublishedProvider> CreateVersion(PublishedProvider publishedProvider, Reference author)
        {
            IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests =
                _publishedProviderVersioningService.AssemblePublishedProviderCreateVersionRequests(new[] { publishedProvider }, author, PublishedProviderStatus.Updated);

            PublishedProviderCreateVersionRequest publishedProviderCreateVersionRequest = publishedProviderCreateVersionRequests.FirstOrDefault();

            if(publishedProviderCreateVersionRequest == null)
            {
                _logger.Warning($"Assign profile pattern key to published provider with ID: {publishedProvider.Id} failed on create new version step.");

                return null;
            }

            return await _publishedProviderVersioningService.CreateVersion(publishedProviderCreateVersionRequest);
        }

        private async Task ProfileFundingLineValues(PublishedProviderVersion newPublishedProviderVersion, ProfilePatternKey profilePatternKey)
        {
            string fundingLineCode = profilePatternKey.FundingLineCode;
            
            FundingLine fundingLine = newPublishedProviderVersion.FundingLines.SingleOrDefault(_ => _.FundingLineCode == fundingLineCode);

            if (fundingLine == null)
            {
                string error = $"Did not locate a funding line with code {fundingLineCode} on published provider version {newPublishedProviderVersion.PublishedProviderId}";
                
                _logger.Error(error);
                
                throw new InvalidOperationException(error);
            }

            if (!fundingLine.Value.HasValue)
            {
                // exit early as there is nothing to profile or re-profile
                return;
            }

            ApiResponse<IEnumerable<ProfileVariationPointer>> variationPointersResponse =
                await _specificationResiliencePolicy.ExecuteAsync(() =>
                    _specificationsApiClient.GetProfileVariationPointers(newPublishedProviderVersion.SpecificationId));

            IEnumerable<ProfileVariationPointer> profileVariationPointers = variationPointersResponse?
                .Content?
                .Where(_ => 
                    _.FundingLineId == fundingLineCode && 
                    _.FundingStreamId == newPublishedProviderVersion.FundingStreamId);

            if (ThereArePaidProfilePeriodsOnTheFundingLine(profileVariationPointers))
            {
                await ReProfileFundingLine(newPublishedProviderVersion, profilePatternKey, fundingLineCode, fundingLine);
            }
            else
            {
                await ProfileFundingLine(fundingLine, newPublishedProviderVersion, profilePatternKey);
            }
        }

        private async Task ReProfileFundingLine(PublishedProviderVersion newPublishedProviderVersion,
            ProfilePatternKey profilePatternKey,
            string fundingLineCode,
            FundingLine fundingLine)
        {
            ReProfileRequest reProfileRequest = await _profilingRequestBuilder.BuildReProfileRequest(fundingLineCode,
                profilePatternKey.Key,
                newPublishedProviderVersion,
                ProfileConfigurationType.Custom,
                fundingLine.Value);

            ReProfileResponse reProfileResponse = (await _profilingPolicy.ExecuteAsync(() => _profiling.ReProfile(reProfileRequest)))?.Content;

            if (reProfileResponse == null)
            {
                string error = $"Unable to re-profile funding line {fundingLineCode} on specification {newPublishedProviderVersion.SpecificationId} with profile pattern {profilePatternKey.Key}";

                _logger.Error(error);

                throw new InvalidOperationException(error);
            }

            fundingLine.DistributionPeriods = _reProfilingResponseMapper.MapReProfileResponseIntoDistributionPeriods(reProfileResponse);

            newPublishedProviderVersion.RemoveCarryOver(fundingLineCode);

            if (reProfileResponse.CarryOverAmount > 0)
            {
                newPublishedProviderVersion.AddCarryOver(fundingLineCode,
                    ProfilingCarryOverType.CustomProfile,
                    reProfileResponse.CarryOverAmount);
            }
        }

        private static bool ThereArePaidProfilePeriodsOnTheFundingLine(IEnumerable<ProfileVariationPointer> profileVariationPointers) => profileVariationPointers != null && profileVariationPointers.Any();

        private async Task ProfileFundingLine(FundingLine fundingLine, PublishedProviderVersion newPublishedProviderVersion, ProfilePatternKey profilePatternKey)
        {
            await _profilingService.ProfileFundingLines(
                    new[] { fundingLine },
                    newPublishedProviderVersion.FundingStreamId,
                    newPublishedProviderVersion.FundingPeriodId,
                    new[] { profilePatternKey });
        }

        private bool MatchingProfilePatternKeyExists(
            PublishedProviderVersion publishedProviderVersion, 
            ProfilePatternKey profilePatternKey)
        {
            return publishedProviderVersion?.ProfilePatternKeys != null && publishedProviderVersion.ProfilePatternKeys.Contains(profilePatternKey);
        }

        private async Task SavePublishedProvider(
            PublishedProvider publishedProvider, 
            PublishedProviderVersion newPublishedProviderVersion)
        {
            HttpStatusCode saveVersionStatusCode = await _publishedProviderVersioningService.SaveVersion(newPublishedProviderVersion);
            ProcessStatusCode(saveVersionStatusCode, $"Failed to save published provider version for id: {newPublishedProviderVersion.Id} with status code {saveVersionStatusCode.ToString()}");

            HttpStatusCode upsertPublishedProviderStatusCode = await _publishingResiliencePolicy.ExecuteAsync(() => _publishedFundingRepository.UpsertPublishedProvider(publishedProvider));
            ProcessStatusCode(upsertPublishedProviderStatusCode, $"Failed to save published provider for id: {publishedProvider.Id} with status code {upsertPublishedProviderStatusCode.ToString()}");
        }

        private void ProcessStatusCode(HttpStatusCode statusCode, string errorMessage)
        {
            if (!statusCode.IsSuccess())
            {
                _logger.Error(errorMessage);

                throw new InvalidOperationException(errorMessage);
            }
        }
    }
}
