using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.FundingDataZone;
using CalculateFunding.Common.ApiClient.FundingDataZone.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Providers;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Providers.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Providers
{
    public class ProviderVersionUpdateCheckService : IProviderVersionUpdateCheckService
    {
        private readonly IPublishingJobClashCheck _publishingJobClashCheck;
        private readonly ICacheProvider _cacheProvider;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly IProviderSnapshotPersistService _providerSnapshotPersistService;
        private readonly IProviderVersionsMetadataRepository _providerVersionMetadata;
        private readonly IFundingDataZoneApiClient _fundingDataZoneApiClient;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        private readonly AsyncPolicy _policiesApiClientPolicy;
        private readonly AsyncPolicy _cacheProviderPolicy;
        private readonly AsyncPolicy _providerVersionMetadataPolicy;
        private readonly AsyncPolicy _fundingDataZoneApiClientPolicy;
        private readonly AsyncPolicy _specificationsApiClientPolicy;

        public ProviderVersionUpdateCheckService(
            IPoliciesApiClient policiesApiClient,
            ICacheProvider cacheProvider,
            ILogger logger,
            IProvidersResiliencePolicies resiliencePolicies,
            IProviderVersionsMetadataRepository providerVersionMetadata,
            IFundingDataZoneApiClient fundingDataZoneApiClient,
            ISpecificationsApiClient specificationsApiClient,
            IMapper mapper,
            IPublishingJobClashCheck publishingJobClashCheck,
            IProviderSnapshotPersistService providerSnapshotPersistService)
        {
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies.ProviderVersionMetadataRepository, nameof(resiliencePolicies.ProviderVersionMetadataRepository));
            Guard.ArgumentNotNull(resiliencePolicies.FundingDataZoneApiClient, nameof(resiliencePolicies.FundingDataZoneApiClient));
            Guard.ArgumentNotNull(resiliencePolicies.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies.CacheProvider, nameof(resiliencePolicies.CacheProvider));
            Guard.ArgumentNotNull(providerVersionMetadata, nameof(providerVersionMetadata));
            Guard.ArgumentNotNull(fundingDataZoneApiClient, nameof(fundingDataZoneApiClient));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(publishingJobClashCheck, nameof(publishingJobClashCheck));
            Guard.ArgumentNotNull(providerSnapshotPersistService, nameof(providerSnapshotPersistService));

            _policiesApiClient = policiesApiClient;
            _cacheProvider = cacheProvider;
            _providerVersionMetadata = providerVersionMetadata;
            _fundingDataZoneApiClient = fundingDataZoneApiClient;
            _specificationsApiClient = specificationsApiClient;
            _mapper = mapper;
            _publishingJobClashCheck = publishingJobClashCheck;
            _logger = logger;
            _cacheProviderPolicy = resiliencePolicies.CacheProvider;
            _policiesApiClientPolicy = resiliencePolicies.PoliciesApiClient;
            _providerVersionMetadataPolicy = resiliencePolicies.ProviderVersionMetadataRepository;
            _fundingDataZoneApiClientPolicy = resiliencePolicies.FundingDataZoneApiClient;
            _specificationsApiClientPolicy = resiliencePolicies.SpecificationsApiClient;
            _providerSnapshotPersistService = providerSnapshotPersistService;
        }

        public async Task CheckProviderVersionUpdate()
        {
            bool disableTrackLatest = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<bool>(CacheKeys.DisableTrackLatest));
            
            if (disableTrackLatest)
            {
                return;
            }

            List<FundingConfiguration> fundingConfigurations = new List<FundingConfiguration>();
            Dictionary<string, int> fundingStreamsLatestProviderSnapshotIds = new Dictionary<string, int>();

            IEnumerable<FundingStream> fundingStreams = await GetFundingStreams();
            IEnumerable<CurrentProviderVersionMetadata> metadataForAllFundingStreams = await GetMetadataForAllFundingStreams();

            foreach (FundingStream fundingStream in fundingStreams)
            {
                if (fundingStream == null)
                {
                    continue;
                }

                string fundingStreamId = fundingStream.Id;

                IEnumerable<FundingConfiguration> fundingStreamConfigurations = await GetFundingConfigurationsByFundingStreamId(fundingStreamId);

                if (!fundingStreamConfigurations.AnyWithNullCheck())
                {
                    continue;
                }

                IEnumerable<FundingConfiguration> periodsWithUpdatesEnabled = fundingStreamConfigurations.Where(_ => _.UpdateCoreProviderVersion == UpdateCoreProviderVersion.ToLatest);

                if (!periodsWithUpdatesEnabled.Any())
                {
                    continue;
                }

                fundingConfigurations.AddRange(periodsWithUpdatesEnabled);

                CurrentProviderVersionMetadata currentProviderVersionMetadata =
                    metadataForAllFundingStreams.SingleOrDefault(_ => _.FundingStreamId == fundingStreamId);
                
                if (currentProviderVersionMetadata == null)
                {
                    string errorMessage = $"Unable to retrieve provider snapshots for funding stream with ID: {fundingStream?.Id}";
                    _logger.Error(errorMessage);
                    continue;
                }

                IEnumerable<ProviderSnapshot> providerSnapshots = await GetLatestProviderSnapshots();
                if (providerSnapshots.IsNullOrEmpty())
                {
                    continue;
                }

                ProviderSnapshot latestProviderSnapshot = providerSnapshots.FirstOrDefault(x => x.FundingStreamCode == fundingStreamId);

                if (latestProviderSnapshot == null)
                {
                    continue;
                }

                int latestProviderSnapshotId = latestProviderSnapshot.ProviderSnapshotId;
                int? currentProviderSnapshotId = currentProviderVersionMetadata.ProviderSnapshotId;

                if (currentProviderSnapshotId != latestProviderSnapshotId)
                {
                    bool success = await _providerSnapshotPersistService.PersistSnapshot(latestProviderSnapshot);

                    if (success)
                    {
                        HttpStatusCode httpStatusCode = await _providerVersionMetadataPolicy.ExecuteAsync(() =>
                               _providerVersionMetadata.UpsertCurrentProviderVersion(
                                   new CurrentProviderVersion
                                   {
                                       Id = $"Current_{latestProviderSnapshot.FundingStreamCode}",
                                       ProviderSnapshotId = latestProviderSnapshot.ProviderSnapshotId,
                                       ProviderVersionId = latestProviderSnapshot.ProviderVersionId
                                   }));

                        success = !httpStatusCode.IsSuccess();
                    }

                    if (success)
                    {
                        string errorMessage = $"Unable to set current provider for funding stream with ID: {fundingStream?.Id}";
                        _logger.Error(errorMessage);
                        continue;
                    }

                    fundingStreamsLatestProviderSnapshotIds.Add(fundingStreamId, latestProviderSnapshotId);

                    LogInformation($"Triggering Update Specification Provider Version for funding stream ID: {fundingStreamId}. " +
                                   $"Latest provider snapshot ID: {latestProviderSnapshotId} " +
                                   $"Current provider version snaphot ID: {currentProviderSnapshotId}");
                }
            }

            if (!fundingStreamsLatestProviderSnapshotIds.Any())
            {
                return;
            }

            IEnumerable<SpecificationSummary> specificationSummaries = await GetSpecificationsWithProviderVersionUpdatesAsUseLatest();

            await EditSpecifications(fundingConfigurations, fundingStreamsLatestProviderSnapshotIds, specificationSummaries);
        }

        private void LogInformation(string message)
        {
            _logger.Information(message);
        }

        private async Task<IEnumerable<CurrentProviderVersionMetadata>> GetMetadataForAllFundingStreams()
        {
            IEnumerable<CurrentProviderVersion> currentProviderVersions =
                await _providerVersionMetadataPolicy.ExecuteAsync(() =>
                    _providerVersionMetadata.GetAllCurrentProviderVersions());

            return _mapper.Map<IEnumerable<CurrentProviderVersionMetadata>>(currentProviderVersions);
        }
        
        private async Task<IEnumerable<ProviderSnapshot>> GetLatestProviderSnapshots()
        {
            ApiResponse<IEnumerable<ProviderSnapshot>> providerSnapshotResponse
                    = await _fundingDataZoneApiClientPolicy.ExecuteAsync(() =>
                        _fundingDataZoneApiClient.GetLatestProviderSnapshotsForAllFundingStreams());

            if (!providerSnapshotResponse.StatusCode.IsSuccess())
            {
                string errorMessage = $"Unable to retrieve latest provider snapshots for funding streams";
                _logger.Error(errorMessage);
                return Enumerable.Empty<ProviderSnapshot>();
            }

            return providerSnapshotResponse.Content;
        }

        private async Task<IEnumerable<FundingConfiguration>> GetFundingConfigurationsByFundingStreamId(string fundingStreamId)
        {
            ApiResponse<IEnumerable<FundingConfiguration>> fundingConfigResponse
                    = await _policiesApiClientPolicy.ExecuteAsync(() =>
                        _policiesApiClient.GetFundingConfigurationsByFundingStreamId(fundingStreamId));

            if (!fundingConfigResponse.StatusCode.IsSuccess())
            {
                string errorMessage = $"Unable to retrieve funding configs for funding stream with ID: {fundingStreamId}";
                _logger.Error(errorMessage);
                return Enumerable.Empty<FundingConfiguration>();
            }

            return fundingConfigResponse.Content;
        }

        private async Task<IEnumerable<FundingStream>> GetFundingStreams()
        {
            ApiResponse<IEnumerable<FundingStream>> fundingStreamsResponse
                = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingStreams());

            if (!fundingStreamsResponse.StatusCode.IsSuccess())
            {
                string errorMessage = "Unable to retrieve funding streams";
                _logger.Error(errorMessage);
                throw new NonRetriableException(errorMessage);
            }

            return fundingStreamsResponse.Content;
        }

        private async Task<IEnumerable<SpecificationSummary>> GetSpecificationsWithProviderVersionUpdatesAsUseLatest()
        {
            ApiResponse<IEnumerable<SpecificationSummary>> specificationsWithProviderVersionUpdateResponse =
                await _specificationsApiClientPolicy.ExecuteAsync(() =>
                    _specificationsApiClient.GetSpecificationsWithProviderVersionUpdatesAsUseLatest());

            if (specificationsWithProviderVersionUpdateResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                string errorMessage = "No specification with provider version updates as use latest";
                _logger.Information(errorMessage);
                return Enumerable.Empty<SpecificationSummary>();
            }

            if (!specificationsWithProviderVersionUpdateResponse.StatusCode.IsSuccess())
            {
                string errorMessage = "Unable to retrieve specification with provider version updates as use latest";
                _logger.Error(errorMessage);
                throw new NonRetriableException(errorMessage);
            }

            return specificationsWithProviderVersionUpdateResponse.Content;
        }

        private async Task EditSpecifications(
            List<FundingConfiguration> fundingConfigurations,
            Dictionary<string, int> fundingStreamsLatestProviderSnapshotIds,
            IEnumerable<SpecificationSummary> specificationSummaries)
        {
            IEnumerable<FundingConfiguration> updateLatestFundingConfigurations =
                fundingConfigurations.Where(_ => _.UpdateCoreProviderVersion == UpdateCoreProviderVersion.ToLatest).ToList();

            foreach (SpecificationSummary specificationSummary in specificationSummaries)
            {
                string fundingStreamId = specificationSummary.FundingStreams.FirstOrDefault().Id;

                int latestProviderSnapshotId = fundingStreamsLatestProviderSnapshotIds[fundingStreamId];

                if (updateLatestFundingConfigurations.Any(_ =>
                    _.FundingPeriodId == specificationSummary.FundingPeriod.Id &&
                    _.FundingStreamId == fundingStreamId) && specificationSummary.ProviderSnapshotId != latestProviderSnapshotId)
                {
                    if (await _publishingJobClashCheck.PublishingJobsClashWithFundingStreamCoreProviderUpdate(specificationSummary.GetSpecificationId()))
                    {
                        LogInformation($"Skipping Update Specification Provider Version for funding stream ID: {fundingStreamId}. " +
                                       $"Latest provider snapshot ID: {latestProviderSnapshotId} as detected publishing jobs running for specifications using an earlier snapshot of this provider data");

                        continue;
                    }

                    await _specificationsApiClientPolicy.ExecuteAsync(() =>
                            _specificationsApiClient.UpdateSpecification(
                                specificationSummary.Id,
                                new EditSpecificationModel
                                {
                                    Name = specificationSummary.Name,
                                    Description = specificationSummary.Description,
                                    FundingStreamId = fundingStreamId,
                                    FundingPeriodId = specificationSummary.FundingPeriod.Id,
                                    ProviderVersionId = specificationSummary.ProviderVersionId,
                                    ProviderSnapshotId = latestProviderSnapshotId,
                                    CoreProviderVersionUpdates = CoreProviderVersionUpdates.UseLatest
                                }));
                }
            }
        }
    }
}
