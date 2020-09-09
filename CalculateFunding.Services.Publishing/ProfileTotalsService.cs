using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
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
                return new OkObjectResult(new Dictionary<string, decimal>());
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

            IEnumerable<PublishedProviderVersion> publishedProviderVersions = await _resilience.ExecuteAsync(() =>
                _publishedFunding.GetPublishedProviderVersions(fundingStreamId, fundingPeriodId, providerId, "Released"));

            return new OkObjectResult(publishedProviderVersions?.ToDictionary(_ => _.Version, 
                _ => new ProfilingVersion { Date = _.Date, 
                    ProfileTotals = new PaymentFundingLineProfileTotals(_),
                    Version = _.Version }) ?? new Dictionary<int, ProfilingVersion>());
        }

        public async Task<IActionResult> GetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
            string specificationId,
            string providerId,
            string fundingStreamId,
            string fundingLineCode)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingLineCode, nameof(fundingLineCode));
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
                    .SingleOrDefault(_ => _.FundingLineCode == fundingLineCode)?.Key,
                ProviderName = latestPublishedProviderVersion.Provider.Name,
                CarryOverAmount = latestPublishedProviderVersion.GetCarryOverTotalForFundingLine(fundingLineCode) ?? 0
            };

            ProfileVariationPointer currentProfileVariationPointer
                = profileVariationPointers.SingleOrDefault(_ => 
                    _.FundingStreamId == fundingStreamId && _.FundingLineId == fundingLineCode);

            ProfileTotal[] profileTotals = new PaymentFundingLineProfileTotals(latestPublishedProviderVersion, fundingLineCode)
                .ToArray();

            fundingLineProfile.TotalAllocation = latestPublishedProviderVersion
                .FundingLines
                .Where(_ => _.Type == OrganisationGroupingReason.Payment)
                .SingleOrDefault(_ => _.FundingLineCode == fundingLineCode)
                ?.Value;

            fundingLineProfile.ProfileTotalAmount = profileTotals.Sum(_ => _.Value);

            FundingDate fundingDate = await _policiesService.GetFundingDate(
                fundingStreamId, 
                latestPublishedProviderVersion.FundingPeriodId, 
                fundingLineCode);

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

            ProfilingAudit latestUpdateProfilingAudit = latestPublishedProviderVersion.GetLatestFundingLineAudit(fundingLineCode);

            fundingLineProfile.LastUpdatedDate = latestUpdateProfilingAudit?.Date;
            fundingLineProfile.LastUpdatedUser = latestUpdateProfilingAudit?.User;

            return new OkObjectResult(fundingLineProfile);
        }

        public async Task<IActionResult> PreviousProfileExistsForSpecificationForProviderForFundingLine(
            string specificationId,
            string providerId,
            string fundingStreamId,
            string fundingLineCode)
        {
            IEnumerable<PublishedProviderVersion> publishedProviderVersions = await _resilience.ExecuteAsync(() =>
                _publishedFunding.GetPublishedProviderVersionsForApproval(
                    specificationId,
                    fundingStreamId,
                    providerId));

            PublishedProviderVersion latestPublishedProviderVersion = publishedProviderVersions.FirstOrDefault();

            if(latestPublishedProviderVersion == null)
            {
                return new NotFoundResult();
            }

            decimal? latestFundingLineValue = latestPublishedProviderVersion
                .FundingLines
                ?.FirstOrDefault(_ => _.FundingLineCode == fundingLineCode)
                .Value;

            decimal? latestCarryOverAmount = latestPublishedProviderVersion
                .CarryOvers
                ?.Where(_ => _.FundingLineCode == fundingLineCode)
                ?.Sum(_ => _.Amount);

            if (FundingLineValueChanged(publishedProviderVersions, fundingLineCode, latestFundingLineValue))
            {
                return new OkObjectResult(true);
            }

            if (CarryOverChanged(publishedProviderVersions, fundingLineCode, latestCarryOverAmount))
            {
                return new OkObjectResult(true);
            }

            return new OkObjectResult(false);
        }

        public async Task<IActionResult> GetPreviousProfilesForSpecificationForProviderForFundingLine(
            string specificationId,
            string providerId,
            string fundingStreamId,
            string fundingLineCode)
        {
            IEnumerable<PublishedProviderVersion> publishedProviderVersions = await _resilience.ExecuteAsync(() =>
                _publishedFunding.GetPublishedProviderVersionsForApproval(
                    specificationId,
                    fundingStreamId,
                    providerId));
            PublishedProviderVersion latestPublishedProviderVersion = publishedProviderVersions.FirstOrDefault();

            if (latestPublishedProviderVersion == null)
            {
                return new NotFoundResult();
            }

            IEnumerable<PublishedProviderVersion> historyPublishedProviderVersions = 
                publishedProviderVersions.Except(new[] { latestPublishedProviderVersion });

            latestPublishedProviderVersion = historyPublishedProviderVersions.FirstOrDefault();

            List<FundingLineChange> fundingLineChanges = new List<FundingLineChange>();

            IEnumerable<FundingStream> fundingStreams = await _policiesService.GetFundingStreams();

            foreach (PublishedProviderVersion publishedProviderVersion in 
                historyPublishedProviderVersions.Except(new[] { latestPublishedProviderVersion }))
            {
                if(publishedProviderVersion.GetFundingLineTotal(fundingLineCode) 
                    != latestPublishedProviderVersion.GetFundingLineTotal(fundingLineCode) ||
                    publishedProviderVersion.GetCarryOverTotalForFundingLine(fundingLineCode)
                    != latestPublishedProviderVersion.GetCarryOverTotalForFundingLine(fundingLineCode))
                {
                    TemplateMetadataDistinctFundingLinesContents templateMetadataDistinctFundingLinesContents =
                        await _policiesService.GetDistinctTemplateMetadataFundingLinesContents(
                            fundingStreamId,
                            latestPublishedProviderVersion.FundingPeriodId,
                            latestPublishedProviderVersion.TemplateVersion);

                    FundingLineChange fundingLineChange = new FundingLineChange
                    {
                        FundingLineTotal = latestPublishedProviderVersion.GetFundingLineTotal(fundingLineCode),
                        PreviousFundingLineTotal = publishedProviderVersion.GetFundingLineTotal(fundingLineCode),
                        FundingStreamName = fundingStreams.SingleOrDefault(_ => _.Id == latestPublishedProviderVersion.FundingStreamId)?.Name,
                        FundingLineName = templateMetadataDistinctFundingLinesContents?.FundingLines?
                            .FirstOrDefault(_ => _.FundingLineCode == fundingLineCode)?.Name,
                        CarryOverAmount = latestPublishedProviderVersion.GetCarryOverTotalForFundingLine(fundingLineCode),
                        LastUpdatedUser = latestPublishedProviderVersion.GetLatestFundingLineAudit(fundingLineCode)?.User,
                        LastUpdatedDate = latestPublishedProviderVersion.Date
                    };

                    ProfileTotal[] profileTotals = new PaymentFundingLineProfileTotals(latestPublishedProviderVersion, fundingLineCode).ToArray();

                    FundingDate fundingDate = await _policiesService.GetFundingDate(
                        fundingStreamId,
                        latestPublishedProviderVersion.FundingPeriodId,
                        fundingLineCode);

                    for (int index = 0; index < profileTotals.Count(); index++)
                    {
                        ProfileTotal profileTotal = profileTotals[index];
                        profileTotal.InstallmentNumber = index + 1;

                        profileTotal.ActualDate = fundingDate?.Patterns?.SingleOrDefault(_ =>
                            _.Occurrence == profileTotal.Occurrence &&
                            _.Period == profileTotal.TypeValue &&
                            _.PeriodYear == profileTotal.Year)?.PaymentDate;
                    }

                    fundingLineChange.ProfileTotals = profileTotals;
                    fundingLineChanges.Add(fundingLineChange);
                }

                latestPublishedProviderVersion = publishedProviderVersion;
            }

            return new OkObjectResult(fundingLineChanges);
        }

        private bool FundingLineValueChanged(
            IEnumerable<PublishedProviderVersion> publishedProviderVersions,
            string fundingLineCode,
            decimal? latestFundingLineValue)
        {
            return publishedProviderVersions.Any(_ =>_.GetFundingLineTotal(fundingLineCode) != latestFundingLineValue);
        }

        private bool CarryOverChanged(
            IEnumerable<PublishedProviderVersion> publishedProviderVersions, 
            string fundingLineCode,
            decimal? latestCarryOverAmount)
        {
            return publishedProviderVersions.Any(_ =>_.GetCarryOverTotalForFundingLine(fundingLineCode) != latestCarryOverAmount);
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