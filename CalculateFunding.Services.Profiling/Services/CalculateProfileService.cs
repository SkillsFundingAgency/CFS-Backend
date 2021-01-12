using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Profiling.Extensions;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Repositories;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Profiling.Services
{
    public class CalculateProfileService : ICalculateProfileService, IHealthChecker
    {
        private readonly IFundingValueProfiler _fundingValueProfiler;
        private readonly IProfilePatternRepository _profilePatternRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly IValidator<ProfileBatchRequest> _batchRequestValidation;
        private readonly IProducerConsumerFactory _producerConsumerFactory;
        private readonly ILogger _logger;
        private readonly AsyncPolicy _profilePatternRepositoryResilience;
        private readonly AsyncPolicy _cachingResilience;

        public CalculateProfileService(IProfilePatternRepository profilePatternRepository,
            ICacheProvider cacheProvider,
            IValidator<ProfileBatchRequest> batchRequestValidation,
            ILogger logger,
            IProfilingResiliencePolicies resiliencePolicies,
            IProducerConsumerFactory producerConsumerFactory,
            IFundingValueProfiler fundingValueProfiler)
        {
            Guard.ArgumentNotNull(profilePatternRepository, nameof(profilePatternRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(batchRequestValidation, nameof(batchRequestValidation));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies?.ProfilePatternRepository, nameof(resiliencePolicies.ProfilePatternRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.Caching, nameof(resiliencePolicies.Caching));
            Guard.ArgumentNotNull(fundingValueProfiler, nameof(fundingValueProfiler));

            _profilePatternRepository = profilePatternRepository;
            _cacheProvider = cacheProvider;
            _logger = logger;
            _producerConsumerFactory = producerConsumerFactory;
            _batchRequestValidation = batchRequestValidation;
            _profilePatternRepositoryResilience = resiliencePolicies.ProfilePatternRepository;
            _cachingResilience = resiliencePolicies.Caching;
            _fundingValueProfiler = fundingValueProfiler;
        }

        public async Task<IActionResult> ProcessProfileAllocationBatchRequest(ProfileBatchRequest profileBatchRequest)
        {
            Guard.ArgumentNotNull(profileBatchRequest, nameof(ProfileBatchRequest));

            ValidationResult validationResult = await _batchRequestValidation.ValidateAsync(profileBatchRequest);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            try
            {
                FundingStreamPeriodProfilePattern profilePattern = await GetProfilePattern(profileBatchRequest);

                BatchProfileRequestContext batchProfileRequestContext = new BatchProfileRequestContext(profilePattern,
                    profileBatchRequest,
                    5);

                IProducerConsumer producerConsumer = _producerConsumerFactory.CreateProducerConsumer(ProduceProviderFundingValues,
                    ProfileProviderFundingValues,
                    10,
                    10,
                    _logger);

                await producerConsumer.Run(batchProfileRequestContext);

                return new OkObjectResult(batchProfileRequestContext.Responses.ToArray());
            }
            catch (Exception ex)
            {
                LogError(ex, profileBatchRequest);

                throw;
            }
        }

        private Task<(bool isComplete, IEnumerable<decimal>)> ProduceProviderFundingValues(CancellationToken token,
            dynamic context)
        {
            BatchProfileRequestContext batchProfileRequestContext = (BatchProfileRequestContext) context;

            while (batchProfileRequestContext.HasPages)
            {
                return Task.FromResult((false, batchProfileRequestContext.NextPage().AsEnumerable()));
            }

            return Task.FromResult((true, ArraySegment<decimal>.Empty.AsEnumerable()));
        }

        private Task ProfileProviderFundingValues(CancellationToken token,
            dynamic context,
            IEnumerable<decimal> providerFundingValues)
        {
            BatchProfileRequestContext batchProfileRequestContext = (BatchProfileRequestContext) context;

            foreach (decimal providerFundingValue in providerFundingValues)
            {
                AllocationProfileResponse allocationProfileResponse = ProfileAllocation(batchProfileRequestContext.Request,
                    batchProfileRequestContext.ProfilePattern,
                    providerFundingValue);

                batchProfileRequestContext.AddResponse(
                    new BatchAllocationProfileResponse(providerFundingValue, allocationProfileResponse));
            }

            return Task.CompletedTask;
        }

        private class BatchProfileRequestContext : PagedContext<decimal>
        {
            private readonly ConcurrentBag<BatchAllocationProfileResponse> _responses
                = new ConcurrentBag<BatchAllocationProfileResponse>();

            public BatchProfileRequestContext(FundingStreamPeriodProfilePattern profilePattern,
                ProfileBatchRequest request,
                int pageSize) : base(request.FundingValues, pageSize)
            {
                Request = request;
                ProfilePattern = profilePattern;
            }

            public ProfileBatchRequest Request { get; }

            public FundingStreamPeriodProfilePattern ProfilePattern { get; }

            public IEnumerable<BatchAllocationProfileResponse> Responses => _responses;

            public void AddResponse(BatchAllocationProfileResponse response)
                => _responses.Add(response);
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

                    return new StatusCodeResult((int) validationResult.Code);
                }

                AllocationProfileResponse profilingResult = ProfileAllocation(profileRequest, profilePattern, profileRequest.FundingValue);
                profilingResult.ProfilePatternKey = profilePattern.ProfilePatternKey;
                profilingResult.ProfilePatternDisplayName = profilePattern.ProfilePatternDisplayName;

                _logger.Information($"Returned Ok for {profileRequest}");

                return new OkObjectResult(profilingResult);
            }
            catch (Exception ex)
            {
                LogError(ex, profileRequest);

                throw;
            }
        }

        private async Task<FundingStreamPeriodProfilePattern> GetProfilePattern(ProfileRequestBase profileRequest) =>
            !string.IsNullOrWhiteSpace(profileRequest.ProviderType) && !string.IsNullOrWhiteSpace(profileRequest.ProviderSubType)
                ? await GetProfilePatternByProviderTypes(profileRequest)
                : await GetProfilePatternByIdOrDefault(profileRequest);

        private string GetProfilePatternCacheKeyById(ProfileRequestBase profileRequest)
        {
            string profilePatternKeyIdComponent = string.IsNullOrWhiteSpace(profileRequest.ProfilePatternKey) ? null : $"-{profileRequest.ProfilePatternKey}";

            return $"{profileRequest.FundingPeriodId}-{profileRequest.FundingStreamId}-{profileRequest.FundingLineCode}{profilePatternKeyIdComponent}";
        }

        private string GetProfilePatternCacheKeyByProviderTypes(ProfileRequestBase profileRequest)
        {
            string providerTypeIdComponent = string.IsNullOrWhiteSpace(profileRequest.ProviderType) ? null : $"-{profileRequest.ProviderType}";
            string providerSubTypeIdComponent = string.IsNullOrWhiteSpace(profileRequest.ProviderSubType) ? null : $"-{profileRequest.ProviderSubType}";

            return $"{profileRequest.FundingPeriodId}-{profileRequest.FundingStreamId}-{profileRequest.FundingLineCode}{providerTypeIdComponent}{providerSubTypeIdComponent}";
        }

        private async Task<FundingStreamPeriodProfilePattern> GetProfilePatternByIdOrDefault(ProfileRequestBase profileRequest)
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

        private async Task<FundingStreamPeriodProfilePattern> GetProfilePatternByProviderTypes(ProfileRequestBase profileRequest)
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

            return profilePattern ?? await GetProfilePatternByIdOrDefault(profileRequest);
        }

        public AllocationProfileResponse ProfileAllocation(
            ProfileRequestBase request,
            FundingStreamPeriodProfilePattern profilePattern,
            decimal fundingValue) =>
            _fundingValueProfiler.ProfileAllocation(request, profilePattern, fundingValue);

        private void LogError(Exception e,
            ProfileRequestBase profileRequest)
        {
            _logger.Error(e, $"Request resulted in an error of: {e.Message} for {profileRequest}");
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(CalculateProfileService)
            };

            ServiceHealth profilePatternRepositoryHealthStatus = await ((IHealthChecker) _profilePatternRepository).IsHealthOk();

            health.Dependencies.AddRange(profilePatternRepositoryHealthStatus.Dependencies);

            return health;
        }
    }
}