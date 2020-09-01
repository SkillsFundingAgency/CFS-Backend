using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Profiling;
using Microsoft.AspNetCore.Mvc;
using Polly;

namespace CalculateFunding.Services.Publishing
{
    public class ProfileTotalsService : IProfileTotalsService
    {
        private readonly AsyncPolicy _resilience;
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly ISpecificationService _specificationService;
        private readonly IPoliciesService _policiesService;

        public ProfileTotalsService(
            IPublishedFundingRepository publishedFunding,
            IPublishingResiliencePolicies resiliencePolicies,
            ISpecificationService specificationService,
            IPoliciesService policiesService)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));

            _resilience = resiliencePolicies.PublishedFundingRepository;
            _publishedFunding = publishedFunding;
            _specificationService = specificationService;
            _policiesService = policiesService;
        }

        public async Task<IActionResult> GetPaymentProfileTotalsForFundingStreamForProvider(string fundingStreamId,
            string fundingPeriodId,
            string providerId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            PublishedProviderVersion latestPublishedProviderVersion = await _resilience.ExecuteAsync(() =>
                _publishedFunding.GetLatestPublishedProviderVersion(fundingStreamId, fundingPeriodId, providerId));

            if (latestPublishedProviderVersion == null)
            {
                return new NotFoundResult();
            }

            ProfileTotal[] profileTotals = new PaymentFundingLineProfileTotals(latestPublishedProviderVersion)
                .ToArray();

            return new OkObjectResult(profileTotals);
        }

        public async Task<IActionResult> GetAllReleasedPaymentProfileTotalsForFundingStreamForProvider(string fundingStreamId,
            string fundingPeriodId,
            string providerId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            PublishedProviderVersion[] publishedProviderVersions = (await _resilience.ExecuteAsync(() =>
                _publishedFunding.GetPublishedProviderVersions(fundingStreamId, fundingPeriodId, providerId, "Released"))).ToArray();

            if (publishedProviderVersions.IsNullOrEmpty())
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(publishedProviderVersions.ToDictionary(_ => _.Version, 
                _ => new ProfilingVersion { Date = _.Date, 
                    ProfileTotals = new PaymentFundingLineProfileTotals(_),
                    Version = _.Version }));
        }

        public async Task<IActionResult> GetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
            string specificationId,
            string providerId,
            string fundingStreamId,
            string fundingLineId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingLineId, nameof(fundingLineId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            PublishedProviderVersion latestPublishedProviderVersion = await _resilience.ExecuteAsync(() =>
                _publishedFunding.GetLatestPublishedProviderVersionBySpecificationId(
                    specificationId,
                    fundingStreamId,
                    providerId));

            if (latestPublishedProviderVersion == null)
            {
                return new NotFoundResult();
            }

            IEnumerable<ProfileVariationPointer> profileVariationPointers 
                = await _specificationService.GetProfileVariationPointers(specificationId);

            if (profileVariationPointers == null)
            {
                return new NotFoundResult();
            }

            FundingLineProfile fundingLineProfile = new FundingLineProfile
            {
                ProfilePatternKey = latestPublishedProviderVersion.ProfilePatternKeys?
                    .SingleOrDefault(_ => _.FundingLineCode == fundingLineId)?.Key,
                ProviderName = latestPublishedProviderVersion.Provider.Name,
                CarryOverAmount = latestPublishedProviderVersion.GetCarryOverTotalForFundingLine(fundingLineId) ?? 0
            };

            ProfileVariationPointer currentProfileVariationPointer
                = profileVariationPointers.SingleOrDefault(_ => 
                    _.FundingStreamId == fundingStreamId && _.FundingLineId == fundingLineId);

            ProfileTotal[] profileTotals = new PaymentFundingLineProfileTotals(latestPublishedProviderVersion, fundingLineId)
                .ToArray();

            fundingLineProfile.TotalAllocation = latestPublishedProviderVersion
                .FundingLines
                .Where(_ => _.Type == OrganisationGroupingReason.Payment)
                .SingleOrDefault(_ => _.FundingLineCode == fundingLineId)
                ?.Value;

            fundingLineProfile.ProfileTotalAmount = profileTotals.Sum(_ => _.Value);

            FundingDate fundingDate = await _policiesService.GetFundingDate(
                fundingStreamId, 
                latestPublishedProviderVersion.FundingPeriodId, 
                fundingLineId);

            for (int index = 0; index < profileTotals.Count(); index++)
            {
                ProfileTotal profileTotal = profileTotals[index];
                profileTotal.InstallmentNumber = index + 1;
                
                profileTotal.IsPaid = IsProfileTotalPaid(currentProfileVariationPointer, profileTotal);
                
                profileTotal.ActualDate = fundingDate?.Patterns?.SingleOrDefault(_ =>
                    _.Occurrence == profileTotal.Occurrence &&
                    _.Period == profileTotal.TypeValue &&
                    _.PeriodYear == profileTotal.Year)?.PaymentDate;
            }

            fundingLineProfile.AmountAlreadyPaid = profileTotals.Where(_ => _.IsPaid).Sum(_ => _.Value);
            fundingLineProfile.RemainingAmount = fundingLineProfile.TotalAllocation - fundingLineProfile.AmountAlreadyPaid;

            foreach (ProfileTotal profileTotal in profileTotals.Where(_ => !_.IsPaid))
            {
                profileTotal.ProfileRemainingPercentage =
                    profileTotal.Value / (fundingLineProfile.TotalAllocation - fundingLineProfile.AmountAlreadyPaid) * 100;
            }

            fundingLineProfile.ProfileTotals = profileTotals;

            ProfilingAudit lastUpdateProfilingAudit = latestPublishedProviderVersion
                .ProfilingAudits
                ?.Where(_ => _.FundingLineCode == fundingLineId)
                ?.OrderByDescending(_ => _.Date)
                ?.FirstOrDefault();

            fundingLineProfile.LastUpdatedDate = lastUpdateProfilingAudit?.Date;
            fundingLineProfile.LastUpdatedUser = lastUpdateProfilingAudit?.User;

            return new OkObjectResult(fundingLineProfile);
        }

        private bool IsProfileTotalPaid(
            ProfileVariationPointer profileVariationPointer,
            ProfileTotal profileTotal)
        {
            if(profileVariationPointer == null)
            {
                return false;
            }

            if(profileTotal.Year > profileVariationPointer.Year || (profileTotal.Year == profileVariationPointer.Year &&
                MonthNumberFor(profileTotal.TypeValue) >= MonthNumberFor(profileVariationPointer.TypeValue)))
            {
                return false;
            }

            return true;
        }

        private static int MonthNumberFor(string monthName)
        {
            return DateTime.ParseExact(monthName, "MMMM", CultureInfo.InvariantCulture)
                .Month * 100;
        }
    }
}