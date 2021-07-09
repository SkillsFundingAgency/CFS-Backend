using AutoMapper;
using CalculateFunding.Common.ApiClient.FundingDataZone;
using CalculateFunding.Common.ApiClient.FundingDataZone.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Providers
{
    public class ProviderSnapshotPersistService : IProviderSnapshotPersistService, IHealthChecker
    {
        private readonly IProviderVersionService _providerVersionService;
        private readonly IFundingDataZoneApiClient _fundingDataZoneApiClient;
        private readonly AsyncPolicy _fundingDataZoneApiClientPolicy;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public ProviderSnapshotPersistService(IProviderVersionService providerVersionService,
            IFundingDataZoneApiClient fundingDataZoneApiClient,
            ILogger logger,
            IMapper mapper,
            IProvidersResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(resiliencePolicies.FundingDataZoneApiClient, nameof(resiliencePolicies.FundingDataZoneApiClient));
            Guard.ArgumentNotNull(fundingDataZoneApiClient, nameof(fundingDataZoneApiClient));
            Guard.ArgumentNotNull(providerVersionService, nameof(providerVersionService));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _providerVersionService = providerVersionService;
            _fundingDataZoneApiClientPolicy = resiliencePolicies.FundingDataZoneApiClient;
            _fundingDataZoneApiClient = fundingDataZoneApiClient;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth versionServiceHealth = await _providerVersionService.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderSnapshotPersistService)
            };

            health.Dependencies.AddRange(versionServiceHealth.Dependencies);

            return health;
        }

        public async Task<bool> PersistSnapshot(ProviderSnapshot providerSnapshot)
        {
            bool isProviderVersionExists = await _providerVersionService.Exists(providerSnapshot.ProviderVersionId);

            if (!isProviderVersionExists)
            {
                IEnumerable<Common.ApiClient.FundingDataZone.Models.Provider> fdzProviders = await GetProvidersInSnapshot(providerSnapshot.ProviderSnapshotId);

                ProviderVersionViewModel providerVersionViewModel = CreateProviderVersionViewModel(providerSnapshot.FundingStreamCode, providerSnapshot.ProviderVersionId, providerSnapshot, fdzProviders);
                (bool success, IActionResult actionResult) = await _providerVersionService.UploadProviderVersion(providerSnapshot.ProviderVersionId, providerVersionViewModel);

                if (!success)
                {
                    string errorMessage = $"Failed to upload provider version {providerSnapshot.ProviderVersionId}. {GetErrorMessage(actionResult, providerSnapshot.ProviderVersionId)}";

                    _logger.Error(errorMessage);

                    throw new Exception(errorMessage);
                }

                return success;
            }
            else
            {
                return true;
            }
        }

        private async Task<IEnumerable<Common.ApiClient.FundingDataZone.Models.Provider>> GetProvidersInSnapshot(int providerSnapshotId)
        {
            ApiResponse<IEnumerable<Common.ApiClient.FundingDataZone.Models.Provider>> providersInSnapshotResponse = await _fundingDataZoneApiClientPolicy.ExecuteAsync(
                            () => _fundingDataZoneApiClient.GetProvidersInSnapshot(providerSnapshotId));

            if (!providersInSnapshotResponse.StatusCode.IsSuccess())
            {
                string errorMessage = $"Unable to retrieve providers for ProviderSnapshotId -{providerSnapshotId}";
                _logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            return providersInSnapshotResponse.Content;
        }

        private ProviderVersionViewModel CreateProviderVersionViewModel(string fundingStreamId, string providerVersionId, ProviderSnapshot providerSnapshot, IEnumerable<Common.ApiClient.FundingDataZone.Models.Provider> fdzProviders)
        {
            IEnumerable<Models.Providers.Provider> providers = fdzProviders.Select(_ =>
            {
                return _mapper.Map<Common.ApiClient.FundingDataZone.Models.Provider, Models.Providers.Provider>(_, opt =>
                    opt.AfterMap((src, dest) =>
                    {
                        dest.ProviderVersionId = providerVersionId;
                        dest.ProviderVersionIdProviderId = $"{providerVersionId}_{dest.ProviderId}";
                    }));
            });

            ProviderVersionViewModel providerVersionViewModel = new ProviderVersionViewModel()
            {
                FundingStream = fundingStreamId,
                ProviderVersionId = providerVersionId,
                VersionType = ProviderVersionType.SystemImported,
                TargetDate = providerSnapshot.TargetDate,
                Created = DateTimeOffset.Now,
                Name = providerSnapshot.Name,
                Description = providerSnapshot.Description,
                Version = 1,
                Providers = providers
            };

            return providerVersionViewModel;
        }
        private string GetErrorMessage(IActionResult actionResult, string providerVersionId)
        {
            return actionResult switch
            {
                ConflictResult _ => $"ProviderVersion alreay exists for - {providerVersionId}",
                BadRequestObjectResult r => $"Validation Errors - {r.AsJson()}",
                _ => string.Empty,
            };
        }
    }
}
