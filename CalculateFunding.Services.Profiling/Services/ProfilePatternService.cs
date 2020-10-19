using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Utility;
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
    public class ProfilePatternService : IProfilePatternService
    {
        private readonly ILogger _logger;
        private readonly AsyncPolicy _patternRepositoryResilience;
        private readonly AsyncPolicy _cacheResilience;
        private readonly IProfilePatternRepository _profilePatterns;
        private readonly ICacheProvider _cacheProvider;
        private readonly IValidator<CreateProfilePatternRequest> _createPatternValidation;
        private readonly IValidator<EditProfilePatternRequest> _upsertPatternValidation;

        public ProfilePatternService(IProfilePatternRepository profilePatterns,
            ICacheProvider cacheProvider,
            IValidator<CreateProfilePatternRequest> createPatternValidation,
            IValidator<EditProfilePatternRequest> upsertPatternValidation,
            IProfilingResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(createPatternValidation, nameof(createPatternValidation));
            Guard.ArgumentNotNull(upsertPatternValidation, nameof(upsertPatternValidation));
            Guard.ArgumentNotNull(profilePatterns, nameof(profilePatterns));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(resiliencePolicies?.ProfilePatternRepository, nameof(resiliencePolicies.ProfilePatternRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.Caching, nameof(resiliencePolicies.Caching));
            
            _logger = logger;
            _cacheProvider = cacheProvider;
            _profilePatterns = profilePatterns;
            _createPatternValidation = createPatternValidation;
            _upsertPatternValidation = upsertPatternValidation;
            _patternRepositoryResilience = resiliencePolicies.ProfilePatternRepository;
            _cacheResilience = resiliencePolicies.Caching;
        }

        public async Task<IActionResult> GetProfilePatterns(string fundingStreamId,
            string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));        
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));    
            
            string cacheKey = CacheKeyForFundingStreamAndFundingPeriod(fundingStreamId, fundingPeriodId);

            IEnumerable<FundingStreamPeriodProfilePattern> profilePatterns = await _cacheResilience.ExecuteAsync(() => 
                _cacheProvider.GetAsync<FundingStreamPeriodProfilePattern[]>(cacheKey));

            if (profilePatterns.IsNullOrEmpty())
            {
                profilePatterns = await _patternRepositoryResilience.ExecuteAsync(() => _profilePatterns.GetProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId,
                    fundingPeriodId));

                if (profilePatterns.IsNullOrEmpty())
                {
                    return new NotFoundResult();
                }
                
                await _cacheResilience.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, profilePatterns.ToArray(), DateTimeOffset.Now.AddMinutes(30)));
            }
            
            return new OkObjectResult(profilePatterns);
        }
        
        public async Task<IActionResult> GetProfilePattern(string id)
        {
            Guard.IsNullOrWhiteSpace(id, nameof(id));

            string cacheKey = CacheKeyForPatternPattern(id);

            FundingStreamPeriodProfilePattern profilePattern = await _cacheResilience.ExecuteAsync(() => 
                _cacheProvider.GetAsync<FundingStreamPeriodProfilePattern>(cacheKey));

            if (profilePattern == null)
            {
                profilePattern = await _patternRepositoryResilience.ExecuteAsync(() => _profilePatterns.GetProfilePattern(id));

                if (profilePattern == null)
                {
                    return new NotFoundResult();
                }
                
                await _cacheResilience.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, profilePattern, DateTimeOffset.Now.AddMinutes(30)));
            }
            
            return new OkObjectResult(profilePattern);
        }
        
        public async Task<FundingStreamPeriodProfilePattern> GetProfilePattern(string fundingStreamId, string fundingPeriodId, string fundingLineCode, string profilePatternKey)
        {
            string streamId = $"{fundingStreamId}-{fundingPeriodId}-{fundingLineCode}{(string.IsNullOrWhiteSpace(profilePatternKey) ? "" : $"-{profilePatternKey}")}";
            
            ActionResult<FundingStreamPeriodProfilePattern> getProfilePattern = await GetProfilePattern(streamId) as OkObjectResult;

            return getProfilePattern?.Value;
        }

        public async Task<IActionResult> CreateProfilePattern(CreateProfilePatternRequest createProfilePatternRequest)
        {
            return await SaveProfilePattern(createProfilePatternRequest, _createPatternValidation);
        }
        
        public async Task<IActionResult> UpsertProfilePattern(EditProfilePatternRequest upsertProfilePatternRequest)
        {
            return await SaveProfilePattern(upsertProfilePatternRequest, _upsertPatternValidation);
        }

        public async Task<IActionResult> DeleteProfilePattern(string id)
        {
            try
            {
                HttpStatusCode statusCodeResult = await _patternRepositoryResilience.ExecuteAsync(() 
                    => _profilePatterns.DeleteProfilePattern(id));

                if (!statusCodeResult.IsSuccess())
                {
                    throw new InvalidOperationException($"Unable to delete profile pattern {id}. Status code {statusCodeResult}");   
                }
                
                await InvalidateProfilePatternCacheEntry(id);
                
                return new StatusCodeResult((int)statusCodeResult);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Unable to delete profile pattern {id}");
                
                throw;    
            }
        }

        private async Task<IActionResult> SaveProfilePattern(ProfilePatternRequestBase request, 
            IValidator validator, 
            [CallerMemberName] string caller = null)
        {
            FundingStreamPeriodProfilePattern profilePattern = request?.Pattern;
            
            try
            {
                ValidationResult validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    return validationResult.AsBadRequest();
                }

                HttpStatusCode result =  await _patternRepositoryResilience.ExecuteAsync(() => _profilePatterns.SaveFundingStreamPeriodProfilePattern(profilePattern));

                if (!result.IsSuccess())
                {
                    throw new InvalidOperationException($"Unable to save profile pattern. StatusCode {result}");
                }

                await InvalidateProfilePatternCacheEntry(profilePattern.Id);
                await InvalidateFundingStreamAndFundingPeriodCacheEntry(profilePattern.FundingStreamId, profilePattern.FundingPeriodId);

                return new OkResult();
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Unable to {caller} {profilePattern?.Id}");
                
                throw;
            }    
        }

        private async Task InvalidateFundingStreamAndFundingPeriodCacheEntry(string fundingStreamId, string fundingPeriodId)
        {
            await _cacheResilience.ExecuteAsync(() => 
                _cacheProvider.RemoveAsync<FundingStreamPeriodProfilePattern[]>(CacheKeyForFundingStreamAndFundingPeriod(fundingStreamId, fundingPeriodId)));
        }

        private async Task InvalidateProfilePatternCacheEntry(string profilePatternId)
        {
            await _cacheResilience.ExecuteAsync(() => 
                _cacheProvider.RemoveAsync<FundingStreamPeriodProfilePattern>(CacheKeyForPatternPattern(profilePatternId)));
        }

        private static string CacheKeyForFundingStreamAndFundingPeriod(string fundingStreamId, string fundingPeriodId)
            => $"{ProfilingCacheKeys.FundingStreamAndPeriod}{fundingStreamId}-{fundingPeriodId}";
        
        private static string CacheKeyForPatternPattern(string profilePatternId) => $"{ProfilingCacheKeys.ProfilePattern}{profilePatternId}";
    }
}