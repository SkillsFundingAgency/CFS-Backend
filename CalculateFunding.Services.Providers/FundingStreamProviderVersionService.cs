using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.Requests;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Providers.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Providers
{
    public class FundingStreamProviderVersionService : IFundingStreamProviderVersionService
    {
        private readonly IProviderVersionsMetadataRepository _providerVersionMetadata;
        private readonly IProviderVersionService _providerVersionService;
        private readonly IProviderVersionSearchService _providerVersionSearch;
        private readonly IValidator<SetFundingStreamCurrentProviderVersionRequest> _setCurrentRequestValidator;
        private readonly AsyncPolicy _providerVersionMetadataPolicy;
        private readonly ILogger _logger;

        public FundingStreamProviderVersionService(IProviderVersionsMetadataRepository providerVersionMetadata,
            IProviderVersionService providerVersionService,
            IProviderVersionSearchService providerVersionSearch,
            IValidator<SetFundingStreamCurrentProviderVersionRequest> setCurrentRequestValidator,
            IProvidersResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(providerVersionMetadata, nameof(providerVersionMetadata));
            Guard.ArgumentNotNull(providerVersionService, nameof(providerVersionService));
            Guard.ArgumentNotNull(providerVersionSearch, nameof(providerVersionSearch));
            Guard.ArgumentNotNull(resiliencePolicies?.ProviderVersionMetadataRepository, nameof(resiliencePolicies.ProviderVersionMetadataRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _providerVersionMetadata = providerVersionMetadata;
            _providerVersionMetadataPolicy = resiliencePolicies.ProviderVersionMetadataRepository;
            _logger = logger;
            _providerVersionSearch = providerVersionSearch;
            _setCurrentRequestValidator = setCurrentRequestValidator;
            _providerVersionService = providerVersionService;
        }

        public async Task<IActionResult> GetCurrentProvidersForFundingStream(string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            CurrentProviderVersion currentProviderVersion = await GetCurrentProviderVersion(fundingStreamId);

            if (currentProviderVersion == null)
            {
                LogError("Unable to get all current providers for funding stream. "+
                         $"No current provider version information located for {fundingStreamId}");
                
                return new NotFoundResult();
            }

            return await _providerVersionService.GetAllProviders(currentProviderVersion.ProviderVersionId);
        }

        public async Task<IActionResult> SetCurrentProviderVersionForFundingStream(string fundingStreamId,
            string providerVersionId)
        {
            ValidationResult validationResult = await _setCurrentRequestValidator.ValidateAsync(new SetFundingStreamCurrentProviderVersionRequest
            {
                FundingStreamId = fundingStreamId,
                ProviderVersionId = providerVersionId
            });

            if (!validationResult.IsValid)
            {
                LogError("The set current provider version for funding stream request was invalid."+ 
                    $"\n{validationResult.Errors.Select(_ => _.ErrorMessage).Join("\n")}");
                
                return validationResult.AsBadRequest();
            }

            await _providerVersionMetadataPolicy.ExecuteAsync(() => _providerVersionMetadata.UpsertCurrentProviderVersion(new CurrentProviderVersion
            {
                Id = $"Current_{fundingStreamId}",
                ProviderVersionId = providerVersionId
            }));
            
            return new NoContentResult();
        }

        public async Task<IActionResult> GetCurrentProviderForFundingStream(string fundingStreamId,
            string providerId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            CurrentProviderVersion currentProviderVersion = await GetCurrentProviderVersion(fundingStreamId);

            if (currentProviderVersion == null)
            {
                LogError("Unable to get current provider for funding stream. "+
                         $"No current provider version information located for {fundingStreamId}");
                
                return new NotFoundResult();
            }

            return await _providerVersionSearch.GetProviderById(currentProviderVersion.ProviderVersionId, providerId);
        }

        public async Task<IActionResult> SearchCurrentProviderVersionsForFundingStream(string fundingStreamId,
            SearchModel search)
        {
            Guard.ArgumentNotNull(search, nameof(search));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            CurrentProviderVersion currentProviderVersion = await GetCurrentProviderVersion(fundingStreamId);
            
            if (currentProviderVersion == null)
            {
                LogError("Unable to search current provider for funding stream. "+
                         $"No current provider version information located for {fundingStreamId}");
                
                return new NotFoundResult();
            }

            return await _providerVersionSearch.SearchProviders(currentProviderVersion.ProviderVersionId, search);
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            Task<ServiceHealth>[] healthChecks = new[]
            {
                ((IHealthChecker) _providerVersionMetadata).IsHealthOk(), 
                _providerVersionService.IsHealthOk()
            };

            await TaskHelper.WhenAllAndThrow(healthChecks);

            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(ProviderVersionService),
            };

            foreach (Task<ServiceHealth> healthCheck in healthChecks)
            {
                health.Dependencies.AddRange(healthCheck.Result.Dependencies);
            }

            return health;
        }

        private async Task<CurrentProviderVersion> GetCurrentProviderVersion(string fundingStreamId) =>
            await _providerVersionMetadataPolicy.ExecuteAsync(() => 
                _providerVersionMetadata.GetCurrentProviderVersion(fundingStreamId));

        private void LogError(string message) => _logger.Warning(message);
    }
}