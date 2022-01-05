using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Profiling.Models;
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
        private readonly IProfilingService _profilingService;

        public ProfileTotalsService(
            IPublishedFundingRepository publishedFunding,
            IPublishingResiliencePolicies resiliencePolicies,
            ISpecificationService specificationService,
            IPoliciesService policiesService,
            IProfilingService profilingService)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(profilingService, nameof(profilingService));

            _resilience = resiliencePolicies.PublishedFundingRepository;
            _publishedFunding = publishedFunding;
            _specificationService = specificationService;
            _policiesService = policiesService;
            _profilingService = profilingService;
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

            IEnumerable<PublishedProviderVersion> publishedProviderVersions = await _resilience.ExecuteAsync(() =>
                _publishedFunding.GetPublishedProviderVersions(fundingStreamId, fundingPeriodId, providerId, "Released"));

            if (publishedProviderVersions == null || publishedProviderVersions.Any() == false)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(publishedProviderVersions?.ToDictionary(_ => _.Version,
                _ => new ProfilingVersion
                {
                    Date = _.Date,
                    ProfileTotals = new PaymentFundingLineProfileTotals(_),
                    Version = _.Version
                }));
        }

        public async Task<ActionResult<FundingLineProfile>> GetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
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

            bool fundingLineExists = latestPublishedProviderVersion.FundingLines.Any(_ => _.FundingLineCode == fundingLineCode);
            if (!fundingLineExists)
            {
                return new NotFoundObjectResult("Funding line not found");
            }

            (IEnumerable<ProfileVariationPointer> profileVariationPointers,
                 TemplateMetadataDistinctFundingLinesContents templateMetadataDistinctFundingLinesContents,
                 IEnumerable<FundingStreamPeriodProfilePattern> fundingStreamPeriodProfilePatterns) = await GetFundingLineProfileData(
                     fundingStreamId,
                     latestPublishedProviderVersion.FundingPeriodId,
                     latestPublishedProviderVersion.TemplateVersion,
                     specificationId);


            FundingLineProfile fundingLineProfile = await GenerateFundingLineProfileModel(
                fundingStreamId,
                fundingLineCode,
                latestPublishedProviderVersion,
                profileVariationPointers,
                templateMetadataDistinctFundingLinesContents,
                fundingStreamPeriodProfilePatterns);

            return fundingLineProfile;
        }

        private async Task<(IEnumerable<ProfileVariationPointer> profileVariationPointers,
            TemplateMetadataDistinctFundingLinesContents templateMetadataDistinctFundingLinesContents,
            IEnumerable<FundingStreamPeriodProfilePattern> fundingStreamPeriodProfilePatterns)> GetFundingLineProfileData(
                string fundingStreamId,
                string fundingPeriodId,
                string templateVersion,
                string specificationId)
        {
            Task<IEnumerable<ProfileVariationPointer>> profileVariationPointers
                = _specificationService.GetProfileVariationPointers(specificationId);

            Task<TemplateMetadataDistinctFundingLinesContents> templateMetadataDistinctFundingLinesContents =
                          _policiesService.GetDistinctTemplateMetadataFundingLinesContents(
                             fundingStreamId,
                             fundingPeriodId,
                             templateVersion);

            Task<IEnumerable<FundingStreamPeriodProfilePattern>> fundingStreamPeriodProfilePatterns =
                 _profilingService.GetProfilePatternsForFundingStreamAndFundingPeriod(
                    fundingStreamId,
                    fundingPeriodId);

            await Task.WhenAll(profileVariationPointers, templateMetadataDistinctFundingLinesContents, fundingStreamPeriodProfilePatterns);

            return (profileVariationPointers.Result, templateMetadataDistinctFundingLinesContents.Result, fundingStreamPeriodProfilePatterns.Result);
        }

        private async Task<FundingLineProfile> GenerateFundingLineProfileModel(
            string fundingStreamId,
            string fundingLineCode,
            PublishedProviderVersion latestPublishedProviderVersion,
            IEnumerable<ProfileVariationPointer> profileVariationPointers,
            TemplateMetadataDistinctFundingLinesContents templateMetadataDistinctFundingLinesContents,
            IEnumerable<FundingStreamPeriodProfilePattern> fundingStreamPeriodProfilePatterns
            )
        {
            FundingLine fundingLine = latestPublishedProviderVersion.FundingLines.SingleOrDefault(_ => _.FundingLineCode == fundingLineCode);

            FundingDate fundingDates = await _policiesService.GetFundingDate(
               fundingStreamId,
               latestPublishedProviderVersion.FundingPeriodId,
               fundingLineCode);

            (string profilePatternKey, string profilePatternName, string profilePatternDescription) =
          GetProfilePatternDetails(fundingLineCode, latestPublishedProviderVersion, fundingStreamPeriodProfilePatterns);

            FundingLineProfile fundingLineProfile = new FundingLineProfile
            {
                FundingLineCode = fundingLineCode,
                FundingLineName = templateMetadataDistinctFundingLinesContents?.FundingLines?
                            .FirstOrDefault(_ => _.FundingLineCode == fundingLineCode)?.Name,
                ProfilePatternKey = profilePatternKey,
                ProfilePatternName = profilePatternName,
                ProfilePatternDescription = profilePatternDescription,
                ProviderId = latestPublishedProviderVersion.ProviderId,
                UKPRN = latestPublishedProviderVersion.Provider.UKPRN,
                ProviderName = latestPublishedProviderVersion.Provider.Name,
                CarryOverAmount = latestPublishedProviderVersion.GetCarryOverTotalForFundingLine(fundingLineCode) ?? 0,
                IsCustomProfile = latestPublishedProviderVersion.FundingLineHasCustomProfile(fundingLineCode),
                FundingLineAmount = fundingLine.Value,
                Errors = latestPublishedProviderVersion.GetErrorsForFundingLine(fundingLineCode, fundingStreamId),
            };

            ProfileVariationPointer currentProfileVariationPointer
                = profileVariationPointers?.SingleOrDefault(_ =>
                    _.FundingStreamId == fundingStreamId && _.FundingLineId == fundingLineCode);

            ProfileTotal[] profileTotals = new PaymentFundingLineProfileTotals(latestPublishedProviderVersion, fundingLineCode)
                .ToArray();

            fundingLineProfile.ProfilePatternTotal = latestPublishedProviderVersion
                .FundingLines
                .Where(_ => _.Type == FundingLineType.Payment)
                .SingleOrDefault(_ => _.FundingLineCode == fundingLineCode)
                ?.Value;

            fundingLineProfile.ProfilePatternTotalWithCarryOver = fundingLineProfile.ProfilePatternTotal;
            if (fundingLineProfile.CarryOverAmount.HasValue && fundingLineProfile.ProfilePatternTotal.HasValue)
            {
                fundingLineProfile.ProfilePatternTotalWithCarryOver += fundingLineProfile.CarryOverAmount.Value;
            }

            for (int index = 0; index < profileTotals.Count(); index++)
            {
                ProfileTotal profileTotal = profileTotals[index];
                profileTotal.InstallmentNumber = index + 1;

                profileTotal.IsPaid = IsProfileTotalPaid(currentProfileVariationPointer, profileTotal);

                profileTotal.ActualDate = fundingDates?.Patterns?.SingleOrDefault(_ =>
                    _.Occurrence == profileTotal.Occurrence &&
                    _.Period == profileTotal.TypeValue &&
                    _.PeriodYear == profileTotal.Year)?.PaymentDate;
            }

            fundingLineProfile.AmountAlreadyPaid = profileTotals.Where(_ => _.IsPaid).Sum(_ => _.Value);
            fundingLineProfile.RemainingAmount = fundingLineProfile.FundingLineAmount - fundingLineProfile.AmountAlreadyPaid;

            foreach (ProfileTotal profileTotal in profileTotals.Where(_ => !_.IsPaid))
            {
                profileTotal.ProfileRemainingPercentage = fundingLineProfile.ProfilePatternTotal.HasValue && (fundingLineProfile.ProfilePatternTotal - fundingLineProfile.AmountAlreadyPaid) != 0 ?
                        profileTotal.Value / (fundingLineProfile.ProfilePatternTotal - fundingLineProfile.AmountAlreadyPaid) * 100 : 0;
            }

            fundingLineProfile.ProfileTotals = profileTotals;

            fundingLineProfile.LastUpdatedDate = latestPublishedProviderVersion.GetLatestFundingLineDate(fundingLineCode);
            fundingLineProfile.LastUpdatedUser = latestPublishedProviderVersion.GetLatestFundingLineUser(fundingLineCode);

            return fundingLineProfile;
        }

        public async Task<IActionResult> GetCurrentProfileConfig
            (string specificationId, string providerId, string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
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

            (IEnumerable<ProfileVariationPointer> profileVariationPointers,
                TemplateMetadataDistinctFundingLinesContents templateMetadataDistinctFundingLinesContents,
                IEnumerable<FundingStreamPeriodProfilePattern> fundingStreamPeriodProfilePatterns) = await GetFundingLineProfileData(
                    fundingStreamId,
                    latestPublishedProviderVersion.FundingPeriodId,
                    latestPublishedProviderVersion.TemplateVersion,
                    specificationId);

            FundingLine[] fundingLines = Enumerable.DistinctBy(latestPublishedProviderVersion
                .FundingLines
                .Where(f => f.Type == FundingLineType.Payment)
                , f => f.FundingLineCode)
                .OrderBy(f => f.Name).ToArray();

            List<FundingLineProfile> fundingLineProfiles = new List<FundingLineProfile>(fundingLines.Length);

            foreach (FundingLine fundingLine in fundingLines)
            {
                FundingLineProfile fundingLineProfile = await GenerateFundingLineProfileModel(fundingStreamId,
                     fundingLine.FundingLineCode,
                     latestPublishedProviderVersion,
                     profileVariationPointers,
                     templateMetadataDistinctFundingLinesContents,
                     fundingStreamPeriodProfilePatterns
                     );

                fundingLineProfiles.Add(fundingLineProfile);
            }

            return new OkObjectResult(fundingLineProfiles);
        }

        public async Task<IActionResult> GetPreviousProfilesForSpecificationForProviderForFundingLine(
            string specificationId,
            string providerId,
            string fundingStreamId,
            string fundingLineCode)
        {
            List<FundingLineChange> fundingLineChanges = new List<FundingLineChange>();

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

            IEnumerable<FundingStream> fundingStreams = await _policiesService.GetFundingStreams();

            foreach (PublishedProviderVersion publishedProviderVersion in historyPublishedProviderVersions)
            {
                if (publishedProviderVersion.GetFundingLineTotal(fundingLineCode)
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
                        LastUpdatedUser = latestPublishedProviderVersion.GetLatestFundingLineUser(fundingLineCode),
                        LastUpdatedDate = latestPublishedProviderVersion.GetLatestFundingLineDate(fundingLineCode),
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

            if (latestPublishedProviderVersion == null)
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

        private static (string key, string name, string description) GetProfilePatternDetails(string fundingLineCode,
            PublishedProviderVersion latestPublishedProviderVersion,
            IEnumerable<FundingStreamPeriodProfilePattern> fundingStreamPeriodProfilePatterns)
        {
            string profilePatternKey = latestPublishedProviderVersion.ProfilePatternKeys?
                .SingleOrDefault(_ => _.FundingLineCode == fundingLineCode)?.Key;

            bool hasCustomProfileForFundingLine = latestPublishedProviderVersion.FundingLineHasCustomProfile(fundingLineCode);

            FundingStreamPeriodProfilePattern apiProfilePatternKey =
                fundingStreamPeriodProfilePatterns.FirstOrDefault(_ => _.ProfilePatternKey == profilePatternKey && _.FundingLineId == fundingLineCode);

            string profilePatternName;
            string profilePatternDescription;

            if (hasCustomProfileForFundingLine)
            {
                profilePatternName = "Custom Profile";
                profilePatternDescription = "Custom Profile";
            }
            else
            {
                profilePatternName = apiProfilePatternKey?.ProfilePatternDisplayName;
                profilePatternDescription = apiProfilePatternKey?.ProfilePatternDescription;
            }

            return (profilePatternKey, profilePatternName, profilePatternDescription);
        }

        private bool FundingLineValueChanged(
            IEnumerable<PublishedProviderVersion> publishedProviderVersions,
            string fundingLineCode,
            decimal? latestFundingLineValue)
        {
            return publishedProviderVersions.Any(_ => _.GetFundingLineTotal(fundingLineCode) != latestFundingLineValue);
        }

        private bool CarryOverChanged(
            IEnumerable<PublishedProviderVersion> publishedProviderVersions,
            string fundingLineCode,
            decimal? latestCarryOverAmount)
        {
            return publishedProviderVersions.Any(_ => _.GetCarryOverTotalForFundingLine(fundingLineCode) != latestCarryOverAmount);
        }

        private bool IsProfileTotalPaid(
            ProfileVariationPointer profileVariationPointer,
            ProfileTotal profileTotal)
        {
            if (profileVariationPointer == null)
            {
                return false;
            }

            if (profileTotal.Year > profileVariationPointer.Year || (profileTotal.Year == profileVariationPointer.Year &&
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