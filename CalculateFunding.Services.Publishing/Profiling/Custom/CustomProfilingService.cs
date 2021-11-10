using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly ISpecificationService _specificationService;
        private readonly IOrganisationGroupService _organisationGroupService;
        private readonly IPoliciesService _policiesService;
        private readonly IProviderService _providerService;

        public CustomProfilingService(
            IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IValidator<ApplyCustomProfileRequest> requestValidation,
            IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies resiliencePolicies,
            IPublishedFundingCsvJobsService publishFundingCsvJobsService,
            ILogger logger,
            ISpecificationService specificationService,
            IOrganisationGroupService organisationGroupService,
            IPoliciesService policiesService,
            IProviderService providerService)
        {
            Guard.ArgumentNotNull(requestValidation, nameof(requestValidation));
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));

            Guard.ArgumentNotNull(publishFundingCsvJobsService, nameof(publishFundingCsvJobsService));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(organisationGroupService, nameof(organisationGroupService));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(providerService, nameof(providerService));

            _publishedProviderVersionCreation = publishedProviderStatusUpdateService;
            _requestValidation = requestValidation;
            _publishedFundingRepository = publishedFundingRepository;
            _logger = logger;
            _specificationService = specificationService;
            _publishedFundingResilience = resiliencePolicies.PublishedFundingRepository;
            _publishFundingCsvJobsService = publishFundingCsvJobsService;
            _organisationGroupService = organisationGroupService;
            _policiesService = policiesService;
            _providerService = providerService;
        }

        public async Task<IActionResult> ApplyCustomProfile(
            ApplyCustomProfileRequest request,
            Reference author,
            string correlationId)
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

            IEnumerable<string> updateRestrictedErrorMessages = await RestrictPastPeriodCustomProfileUpdate(request, publishedProvider);
            if (!updateRestrictedErrorMessages.IsNullOrEmpty())
            {
                return new BadRequestObjectResult(
                    updateRestrictedErrorMessages.ToArray().ToModelStateDictionary());
            }

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
                correlationId: correlationId,
                force: true);

            _logger.Information(
                $"Successfully applied custom profiling {request.CustomProfileName} to published provider {publishedProviderId}");

            await _publishFundingCsvJobsService.QueueCsvJobs(GeneratePublishingCsvJobsCreationAction.Refresh,
                currentProviderVersion.SpecificationId,
                correlationId,
                author);

            return new NoContentResult();
        }

        private async Task<IEnumerable<string>> RestrictPastPeriodCustomProfileUpdate(
            ApplyCustomProfileRequest request,
            PublishedProvider publishedProvider)
        {
            string specificationId = publishedProvider.Current.SpecificationId;

            SpecificationSummary specificationSummary =
                await _specificationService.GetSpecificationSummaryById(specificationId);

            IEnumerable<ProfileVariationPointer> profileVariationPointers
                = await _specificationService.GetProfileVariationPointers(specificationId);

            if (!profileVariationPointers.AnyWithNullCheck())
            {
                return Array.Empty<string>();
            }

            FundingConfiguration fundingConfiguration
                = await _policiesService.GetFundingConfiguration(
                    specificationSummary.FundingStreams.FirstOrDefault().Id,
                    specificationSummary.FundingPeriod.Id);

            IDictionary<string, Provider> scopedProviders
                = await _providerService.GetScopedProvidersForSpecification(
                    specificationSummary.Id, specificationSummary.ProviderVersionId);

            Dictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData =
                await _organisationGroupService.GenerateOrganisationGroups(
                        scopedProviders.Values,
                        new[] { publishedProvider },
                        fundingConfiguration,
                        specificationSummary.ProviderVersionId,
                        specificationSummary.ProviderSnapshotId);

            IEnumerable<OrganisationGroupingReason> organisationGroupingReasons
                = organisationGroupResultsData.Values.SelectMany(v => v.Select(_ => _.GroupReason)).Distinct();

            if (organisationGroupingReasons.Any(_ => !IsContracted(_)))
            {
                ProfileVariationPointer latestProfileVariationPointer = profileVariationPointers
                    .OrderByDescending(_ => _.Year)
                    .ThenByDescending(_ => YearMonthOrderedProfilePeriods.MonthNumberFor(_.TypeValue))
                    .FirstOrDefault();

                ProfilePeriod earliestProfilePeriod = request.ProfilePeriods
                    .OrderBy(_ => _.Year)
                    .ThenBy(_ => YearMonthOrderedProfilePeriods.MonthNumberFor(_.TypeValue))
                    .FirstOrDefault();

                if (earliestProfilePeriod.Year < latestProfileVariationPointer.Year ||
                    (earliestProfilePeriod.Year == latestProfileVariationPointer.Year
                        && YearMonthOrderedProfilePeriods.MonthNumberFor(earliestProfilePeriod.TypeValue) < YearMonthOrderedProfilePeriods.MonthNumberFor(latestProfileVariationPointer.TypeValue)))
                {
                    return new string[] {
                        $"Updating past profile periods for non contracted providers are restricted for custom profiling." +
                        $"Profile Variation Pointer: Year={latestProfileVariationPointer.Year} Month={latestProfileVariationPointer.TypeValue} " +
                        $"Profile Period Request: Year={earliestProfilePeriod.Year} Month={earliestProfilePeriod.TypeValue}"};
                }
            }

            return Array.Empty<string>();
        }

        private static bool IsContracted(OrganisationGroupingReason organisationGroupingReason)
        {
            return organisationGroupingReason == OrganisationGroupingReason.Contracting;
        }
    }
}