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
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderProfilingService : IPublishedProviderProfilingService
    {
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly ILogger _logger;
        private readonly AsyncPolicy _publishingResiliencePolicy;
        private readonly AsyncPolicy _specificationResiliencePolicy;
        private readonly IPublishedProviderErrorDetection _publishedProviderErrorDetection;
        private readonly IProfilingService _profilingService;
        private readonly IPublishedProviderVersioningService _publishedProviderVersioningService;
        private readonly ISpecificationsApiClient _specificationsApiClient;

        public PublishedProviderProfilingService(
            IPublishedFundingRepository publishedFundingRepository,
            IPublishedProviderErrorDetection publishedProviderErrorDetection,
            IProfilingService profilingService,
            IPublishedProviderVersioningService publishedProviderVersioningService,
            ISpecificationsApiClient specificationsApiClient,
            ILogger logger,
            IPublishingResiliencePolicies publishingResiliencePolicies)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishedProviderErrorDetection, nameof(publishedProviderErrorDetection));
            Guard.ArgumentNotNull(profilingService, nameof(profilingService));
            Guard.ArgumentNotNull(publishedProviderVersioningService, nameof(publishedProviderVersioningService));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.SpecificationsApiClient, nameof(publishingResiliencePolicies.SpecificationsApiClient));

            _publishedFundingRepository = publishedFundingRepository;
            _publishedProviderErrorDetection = publishedProviderErrorDetection;
            _profilingService = profilingService;
            _publishedProviderVersioningService = publishedProviderVersioningService;
            _specificationsApiClient = specificationsApiClient;
            _logger = logger;
            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _specificationResiliencePolicy = publishingResiliencePolicies.SpecificationsApiClient;
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

            if (MatchingProfilePatternKeyExists(publishedProvider.Current, profilePatternKey))
            {
                return new StatusCodeResult((int)HttpStatusCode.NotModified);
            }

            PublishedProvider modifiedPublishedProvider = await CreateVersion(publishedProvider, author);

            if(modifiedPublishedProvider == null)
            {
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            PublishedProviderVersion newPublishedProviderVersion = publishedProvider.Current;

            SetProfilePatternKey(newPublishedProviderVersion, profilePatternKey);

            await ProfileFundingLineValues(newPublishedProviderVersion, profilePatternKey);

            await _publishedProviderErrorDetection.ProcessPublishedProvider(newPublishedProviderVersion);

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
            FundingLine fundingLine = newPublishedProviderVersion.FundingLines.SingleOrDefault(_ => _.FundingLineCode == profilePatternKey.FundingLineCode);

            if (fundingLine == null)
            {
                return;
            }

            ApiResponse<IEnumerable<ProfileVariationPointer>> variationPointersResponse =
                await _specificationResiliencePolicy.ExecuteAsync(() =>
                    _specificationsApiClient.GetProfileVariationPointers(newPublishedProviderVersion.SpecificationId));

            IEnumerable<ProfileVariationPointer> profileVariationPointers = variationPointersResponse?
                .Content?
                .Where(_ => 
                    _.FundingLineId == profilePatternKey.FundingLineCode && 
                    _.FundingStreamId == newPublishedProviderVersion.FundingStreamId);

            if (profileVariationPointers != null && profileVariationPointers.Any())
            {
                FundingLine modifiedFundingLine = fundingLine.Clone();

                IEnumerable<ProfilePeriod> paidProfilePeriods = modifiedFundingLine
                    .DistributionPeriods
                    .SelectMany(dp => dp.ProfilePeriods.Where(profilePeriod => HasMatchingProfileVariationPointer(profilePeriod, profileVariationPointers)))
                    .ToList();

                RemovePaidProfilePeriods(modifiedFundingLine, paidProfilePeriods);
                await ProfileFundingLines(modifiedFundingLine, newPublishedProviderVersion, profilePatternKey);
                AddPaidProfilePeriods(modifiedFundingLine, paidProfilePeriods);

                fundingLine = modifiedFundingLine;
            }
            else
            {
                await ProfileFundingLines(fundingLine, newPublishedProviderVersion, profilePatternKey);
            }
        }

        private async Task ProfileFundingLines(FundingLine fundingLine, PublishedProviderVersion newPublishedProviderVersion, ProfilePatternKey profilePatternKey)
        {
            await _profilingService.ProfileFundingLines(
                    new[] { fundingLine },
                    newPublishedProviderVersion.FundingStreamId,
                    newPublishedProviderVersion.FundingPeriodId,
                    profilePatternKey.Key);
        }

        private void RemovePaidProfilePeriods(FundingLine fundingLine, IEnumerable<ProfilePeriod> paidProfilePeriods)
        {
            foreach (ProfilePeriod profilePeriod in paidProfilePeriods)
            {
                (fundingLine
                    .DistributionPeriods
                    .SingleOrDefault(_ => _.DistributionPeriodId == profilePeriod.DistributionPeriodId)
                    ?.ProfilePeriods as List<ProfilePeriod>)
                    ?.Remove(profilePeriod);
            }
        }

        private void AddPaidProfilePeriods(FundingLine fundingLine, IEnumerable<ProfilePeriod> paidProfilePeriods)
        {
            foreach (ProfilePeriod profilePeriod in paidProfilePeriods)
            {
                DistributionPeriod distributionPeriod = fundingLine
                    .DistributionPeriods
                    .SingleOrDefault(_ => _.DistributionPeriodId == profilePeriod.DistributionPeriodId);

                if (distributionPeriod == null)
                {
                    distributionPeriod = new DistributionPeriod
                    {
                        DistributionPeriodId = profilePeriod.DistributionPeriodId,
                        Value = paidProfilePeriods.Where(_ => _.DistributionPeriodId == profilePeriod.DistributionPeriodId).Select(_ => _.ProfiledValue).Sum(),
                        ProfilePeriods = new List<ProfilePeriod> { profilePeriod }
                    };

                    (fundingLine
                        .DistributionPeriods as List<DistributionPeriod>)
                        .Add(distributionPeriod);
                }
                else
                {
                    (distributionPeriod.ProfilePeriods as List<ProfilePeriod>)?.Add(profilePeriod);
                }
            }
        }

        private bool HasMatchingProfileVariationPointer(ProfilePeriod profilePeriod, IEnumerable<ProfileVariationPointer> profileVariationPointers)
        {
            return profileVariationPointers.Any(_ =>
               _.PeriodType == profilePeriod.Type.ToString() &&
               _.TypeValue == profilePeriod.TypeValue &&
               _.Year == profilePeriod.Year &&
               _.Occurrence == profilePeriod.Occurrence);
        }

        private bool MatchingProfilePatternKeyExists(
            PublishedProviderVersion publishedProviderVersion, 
            ProfilePatternKey profilePatternKey)
        {
            return publishedProviderVersion?.ProfilePatternKeys != null && publishedProviderVersion.ProfilePatternKeys.Contains(profilePatternKey);
        }

        private void SetProfilePatternKey(
            PublishedProviderVersion publishedProviderVersion, 
            ProfilePatternKey profilePatternKey)
        {
            if (publishedProviderVersion.ProfilePatternKeys?.Any(_ => _.FundingLineCode == profilePatternKey.FundingLineCode) == true)
            {
                publishedProviderVersion.ProfilePatternKeys
                    .SingleOrDefault(_ => _.FundingLineCode == profilePatternKey.FundingLineCode)
                    .Key = profilePatternKey.Key;
            }
            else
            {
                if(publishedProviderVersion.ProfilePatternKeys == null)
                {
                    publishedProviderVersion.ProfilePatternKeys = new List<ProfilePatternKey>();
                }

                publishedProviderVersion.ProfilePatternKeys.Add(profilePatternKey);
            }
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
