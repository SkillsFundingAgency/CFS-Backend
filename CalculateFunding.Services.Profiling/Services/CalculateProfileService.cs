using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Profiling.Extensions;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Repositories;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Profiling.Services
{
    public class CalculateProfileService : ICalculateProfileService, IHealthChecker
    {
        private readonly IProfilePatternRepository _profilePatternRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly ILogger _logger;
        private readonly AsyncPolicy _profilePatternRepositoryResilience;
        private readonly AsyncPolicy _cachingResilience;

        public CalculateProfileService(
            IProfilePatternRepository profilePatternRepository,
            ICacheProvider cacheProvider,
            ILogger logger,
            IProfilingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(profilePatternRepository, nameof(profilePatternRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies?.ProfilePatternRepository, nameof(resiliencePolicies.ProfilePatternRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.Caching, nameof(resiliencePolicies.Caching));

            _profilePatternRepository = profilePatternRepository;
            _cacheProvider = cacheProvider;
            _logger = logger;
            _profilePatternRepositoryResilience = resiliencePolicies.ProfilePatternRepository;
            _cachingResilience = resiliencePolicies.Caching;
        }

        public async Task<IActionResult> ProcessProfileAllocationRequest(ProfileRequest profileRequest)
        {
            Guard.ArgumentNotNull(profileRequest, nameof(profileRequest));

            _logger.Information($"Retrieved a request {profileRequest}");
            try
            {
                FundingStreamPeriodProfilePattern profilePattern = await GetProfilePattern(profileRequest);

                ProfileValidationResult validationResult =
                    ProfileRequestValidator.ValidateRequestAgainstPattern(profileRequest, profilePattern);

                if (validationResult.Code != HttpStatusCode.OK)
                {
                    _logger.Information($"Returned status code of {validationResult.Code} for {profileRequest}");

                    return new StatusCodeResult((int)validationResult.Code);
                }

                AllocationProfileResponse profilingResult = ProfileAllocation(profileRequest, profilePattern);
                profilingResult.ProfilePatternKey = profilePattern.ProfilePatternKey;
                profilingResult.ProfilePatternDisplayName = profilePattern.ProfilePatternDisplayName;

                _logger.Information($"Returned Ok for {profileRequest}");

                return new OkObjectResult(profilingResult);
            }
            catch (Exception e)
            {
                _logger.Error($"Request resulted in an error of: {e.Message} for {profileRequest}");

                throw;
            }
        }

        private async Task<FundingStreamPeriodProfilePattern> GetProfilePattern(ProfileRequest profileRequest)
        {
            return !string.IsNullOrWhiteSpace(profileRequest.ProviderType) && !string.IsNullOrWhiteSpace(profileRequest.ProviderSubType)
                ? await GetProfilePatternByProviderTypes(profileRequest)
                : await GetProfilePatternByIdOrDefault(profileRequest);
        }

        private string GetProfilePatternCacheKeyById(ProfileRequest profileRequest)
        {
            string profilePatternKeyIdComponent = string.IsNullOrWhiteSpace(profileRequest.ProfilePatternKey) ? null : $"-{profileRequest.ProfilePatternKey}";

            return $"{profileRequest.FundingPeriodId}-{profileRequest.FundingStreamId}-{profileRequest.FundingLineCode}{profilePatternKeyIdComponent}";
        }

        private string GetProfilePatternCacheKeyByProviderTypes(ProfileRequest profileRequest)
        {
            string providerTypeIdComponent = string.IsNullOrWhiteSpace(profileRequest.ProviderType) ? null : $"-{profileRequest.ProviderType}";
            string providerSubTypeIdComponent = string.IsNullOrWhiteSpace(profileRequest.ProviderSubType) ? null : $"-{profileRequest.ProviderSubType}";

            return $"{profileRequest.FundingPeriodId}-{profileRequest.FundingStreamId}-{profileRequest.FundingLineCode}{providerTypeIdComponent}{providerSubTypeIdComponent}";
        }

        private async Task<FundingStreamPeriodProfilePattern> GetProfilePatternByIdOrDefault(ProfileRequest profileRequest)
        {
            string profilePatternCacheKey = GetProfilePatternCacheKeyById(profileRequest);

            FundingStreamPeriodProfilePattern profilePattern = await _cachingResilience.ExecuteAsync(() => _cacheProvider.GetAsync<FundingStreamPeriodProfilePattern>(profilePatternCacheKey));

            if (profilePattern == null)
            {
                profilePattern = await _profilePatternRepositoryResilience.ExecuteAsync(() =>
                    _profilePatternRepository.GetProfilePattern(profileRequest.FundingPeriodId,
                        profileRequest.FundingStreamId,
                        profileRequest.FundingLineCode,
                        profileRequest.ProfilePatternKey));

                if (profilePattern != null)
                {
                    await _cachingResilience.ExecuteAsync(() => _cacheProvider.SetAsync(profilePatternCacheKey, profilePattern, DateTimeOffset.Now.AddMinutes(30)));
                }
            }

            return profilePattern;
        }

        private async Task<FundingStreamPeriodProfilePattern> GetProfilePatternByProviderTypes(ProfileRequest profileRequest)
        {
            FundingStreamPeriodProfilePattern profilePattern = null;

            string profilePatternCacheKey = GetProfilePatternCacheKeyByProviderTypes(profileRequest);

            profilePattern = await _cachingResilience.ExecuteAsync(() => _cacheProvider.GetAsync<FundingStreamPeriodProfilePattern>(profilePatternCacheKey));

            if (profilePattern == null)
            {
                profilePattern = await _profilePatternRepositoryResilience.ExecuteAsync(() =>
                _profilePatternRepository.GetProfilePattern(profileRequest.FundingPeriodId,
                    profileRequest.FundingStreamId,
                    profileRequest.FundingLineCode,
                    profileRequest.ProviderType,
                    profileRequest.ProviderSubType));

                if (profilePattern != null)
                {
                    await _cachingResilience.ExecuteAsync(() => _cacheProvider.SetAsync(profilePatternCacheKey, profilePattern, DateTimeOffset.Now.AddMinutes(30)));
                }
            }

            if (profilePattern == null)
            {
                profilePattern = await GetProfilePatternByIdOrDefault(profileRequest);
            }

            return profilePattern;
        }

        public AllocationProfileResponse ProfileAllocation(
            ProfileRequest request, FundingStreamPeriodProfilePattern profilePattern)
        {

            IReadOnlyCollection<DeliveryProfilePeriod> profilePeriods = GetProfiledAllocationPeriodsWithPatternApplied(
                 request, profilePattern.ProfilePattern, profilePattern.RoundingStrategy);

            IReadOnlyCollection<DistributionPeriods> distributionPeriods = GetDistributionPeriodWithPatternApplied(
                profilePeriods);

            return new AllocationProfileResponse(
                profilePeriods.ToArray(),
                distributionPeriods.ToArray());
        }

        private IReadOnlyCollection<DistributionPeriods> GetDistributionPeriodWithPatternApplied(
            IReadOnlyCollection<DeliveryProfilePeriod> profilePattern)
        {
            IReadOnlyCollection<DeliveryProfilePeriod> allocationProfilePeriods =
                GetDistributionPeriodForAllocation(profilePattern);

            return ApplyDistributionPeriodsProfilePattern(allocationProfilePeriods);
        }

        private IReadOnlyCollection<DeliveryProfilePeriod> GetDistributionPeriodForAllocation(
           IReadOnlyCollection<DeliveryProfilePeriod> profilePattern)
        {
            return profilePattern
                .Select(ppp => new DeliveryProfilePeriod(ppp.TypeValue, ppp.Occurrence, ppp.Type, ppp.Year, ppp.ProfileValue, ppp.DistributionPeriod))
                .ToList();
        }

        private IReadOnlyCollection<DistributionPeriods> ApplyDistributionPeriodsProfilePattern(

           IReadOnlyCollection<DeliveryProfilePeriod> profilePeriods)
        {
            List<DistributionPeriods> calculatedDeliveryProfile = new List<DistributionPeriods>();


            if (profilePeriods.Any())
            {
                IReadOnlyCollection<TotalByDistributionPeriod> totalByDistributionPeriod =
                    GetTotalDistributionPeriods(profilePeriods);

                foreach (var requestPeriod in totalByDistributionPeriod)
                {
                    calculatedDeliveryProfile.Add(new DistributionPeriods()
                    {
                        Value = requestPeriod.Value,
                        DistributionPeriodCode = requestPeriod.DistributionPeriodCode
                    });
                }
            }
            return calculatedDeliveryProfile;
        }

        private IReadOnlyCollection<TotalByDistributionPeriod> GetTotalDistributionPeriods(

           IReadOnlyCollection<DeliveryProfilePeriod> profilePeriods)
        {
            return profilePeriods
                .Select(p => p.DistributionPeriod)
                .Distinct()
                .Select(distributionPeriod => GetTotalForDistributionPeriod(profilePeriods, distributionPeriod))
                .ToList();
        }

        private TotalByDistributionPeriod GetTotalForDistributionPeriod(
           IReadOnlyCollection<DeliveryProfilePeriod> profilePeriods,
           string distributionPeriod)
        {
            IReadOnlyCollection<DeliveryProfilePeriod> matchedPatterns =
                GetMatchingProfilePatterns(profilePeriods, distributionPeriod);

            return new TotalByDistributionPeriod(distributionPeriod, matchedPatterns.Sum(mp => mp.ProfileValue));
        }

        private IReadOnlyCollection<DeliveryProfilePeriod> GetMatchingProfilePatterns(IReadOnlyCollection<DeliveryProfilePeriod> periods, string distributionPeriod)
        {

            return periods.Where(period =>
                   period.DistributionPeriod == distributionPeriod)
                .ToList();
        }

        private IReadOnlyCollection<DeliveryProfilePeriod> GetProfiledAllocationPeriodsWithPatternApplied(
            ProfileRequest profileRequest,
            IReadOnlyCollection<ProfilePeriodPattern> profilePattern,
            RoundingStrategy roundingStrategy)
        {
            IReadOnlyCollection<DeliveryProfilePeriod> allocationProfilePeriods =
                GetProfilePeriodsForAllocation(profilePattern);

            return ApplyProfilePattern(profileRequest, profilePattern, allocationProfilePeriods, roundingStrategy);
        }

        private IReadOnlyCollection<DeliveryProfilePeriod> GetProfilePeriodsForAllocation(
             IReadOnlyCollection<ProfilePeriodPattern> profilePattern)
        {
            return profilePattern
                .Select(ppp => new DeliveryProfilePeriod(ppp.Period, ppp.Occurrence, ppp.PeriodType, ppp.PeriodYear, 0m, ppp.DistributionPeriod))
                .Distinct()
                .ToList();
        }

        private IReadOnlyCollection<DeliveryProfilePeriod> ApplyProfilePattern(
            ProfileRequest profileRequest,
            IReadOnlyCollection<ProfilePeriodPattern> profilePattern,
            IReadOnlyCollection<DeliveryProfilePeriod> profilePeriods,
            RoundingStrategy roundingStrategy)
        {
            List<DeliveryProfilePeriod> calculatedDeliveryProfile = new List<DeliveryProfilePeriod>();

            if (profilePeriods.Any())
            {
                decimal allocationValueToBeProfiled = Convert.ToDecimal(profileRequest.FundingValue);

                List<DeliveryProfilePeriod> profiledValues = profilePeriods.Select(pp =>
                {
                    ProfilePeriodPattern profilePeriodPattern = profilePattern.Single(
                        pattern => string.Equals(pattern.Period, pp.TypeValue)
                                   && string.Equals(pattern.DistributionPeriod, pp.DistributionPeriod)
                                   && pattern.Occurrence == pp.Occurrence);

                    decimal profilePercentage = profilePeriodPattern
                        .PeriodPatternPercentage;

                    decimal profiledValue = (profilePercentage * allocationValueToBeProfiled);
                    if (profiledValue != 0)
                    {
                        profiledValue /= 100;
                    }

                    decimal roundedValue;


                    if (roundingStrategy == RoundingStrategy.RoundUp)
                    {
                        roundedValue = profiledValue
                            .RoundToDecimalPlaces(2)
                            .RoundToDecimalPlaces(0);
                    }
                    else
                    {
                        roundedValue = (int)profiledValue;
                    }

                    return pp.WithValue(roundedValue);
                }).ToList();

                DeliveryProfilePeriod last = profiledValues.Last();

                IEnumerable<DeliveryProfilePeriod> withoutLast = profiledValues.Take(profiledValues.Count - 1).ToList();

                calculatedDeliveryProfile.AddRange(
                    withoutLast.Append(
                        last.WithValue(allocationValueToBeProfiled - withoutLast.Sum(cdp => cdp.ProfileValue))));
            }

            return calculatedDeliveryProfile;
        }

        private class TotalByDistributionPeriod
        {
            public TotalByDistributionPeriod(string distributionPeriodCode, decimal value)
            {
                DistributionPeriodCode = distributionPeriodCode;
                Value = value;
            }

            public string DistributionPeriodCode { get; }

            public decimal Value { get; }
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(CalculateProfileService)
            };

            ServiceHealth profilePatternRepositoryHealthStatus = await ((IHealthChecker)_profilePatternRepository).IsHealthOk();

            health.Dependencies.AddRange(profilePatternRepositoryHealthStatus.Dependencies);

            return health;
        }
    }
}