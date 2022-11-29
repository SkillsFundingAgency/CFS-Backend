using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.FundingDataZone;
using CalculateFunding.Common.ApiClient.FundingDataZone.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Providers;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Providers.Interfaces;
using Polly;
using Serilog;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Services.Core.Constants;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Providers
{
    public class ProviderVersionUpdateCheckService : IProviderVersionUpdateCheckService
    {
        private readonly ICacheProvider _cacheProvider;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly IProviderVersionsMetadataRepository _providerVersionMetadata;
        private readonly IFundingDataZoneApiClient _fundingDataZoneApiClient;
        private readonly IJobManagement _jobManagement;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        private readonly AsyncPolicy _policiesApiClientPolicy;
        private readonly AsyncPolicy _cacheProviderPolicy;
        private readonly AsyncPolicy _providerVersionMetadataPolicy;
        private readonly AsyncPolicy _fundingDataZoneApiClientPolicy;

        public ProviderVersionUpdateCheckService(
            IPoliciesApiClient policiesApiClient,
            ICacheProvider cacheProvider,
            ILogger logger,
            IProvidersResiliencePolicies resiliencePolicies,
            IProviderVersionsMetadataRepository providerVersionMetadata,
            IFundingDataZoneApiClient fundingDataZoneApiClient,
            IJobManagement jobManagement,
            IMapper mapper)
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
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _policiesApiClient = policiesApiClient;
            _cacheProvider = cacheProvider;
            _providerVersionMetadata = providerVersionMetadata;
            _fundingDataZoneApiClient = fundingDataZoneApiClient;
            _jobManagement = jobManagement;
            _mapper = mapper;
            _logger = logger;
            _cacheProviderPolicy = resiliencePolicies.CacheProvider;
            _policiesApiClientPolicy = resiliencePolicies.PoliciesApiClient;
            _providerVersionMetadataPolicy = resiliencePolicies.ProviderVersionMetadataRepository;
            _fundingDataZoneApiClientPolicy = resiliencePolicies.FundingDataZoneApiClient;
        }

        public async Task CheckProviderVersionUpdate()
        {
            bool disableTrackLatest = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<bool>(CacheKeys.DisableTrackLatest));

            if (disableTrackLatest)
            {
                return;
            }

            List<FundingConfiguration> fundingConfigurations = new List<FundingConfiguration>();
           
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

                IEnumerable<ProviderSnapshot> providerSnapshots = await GetLatestProviderSnapshots();
                if (providerSnapshots.IsNullOrEmpty())
                {
                    continue;
                }

                IEnumerable<ProviderSnapshot> SnapshotIdWithFundingPeriod = await GetLatestProviderSnapshotsForAllFundingStreamsWithFundingPeriod();

                if (SnapshotIdWithFundingPeriod.IsNullOrEmpty())
                {
                    continue;
                }

                var latestproviderSnapShotByFundingPeriod = SnapshotIdWithFundingPeriod.Where(_ => _.FundingStreamCode == fundingStreamId)
                    .Select(_ => new ProviderSnapShotByFundingPeriod() { FundingPeriodName = _.FundingPeriodName, ProviderSnapshotId = _.ProviderSnapshotId }).ToList();

                var latestfundingPeriodWithSnapShotId = JsonConvert.SerializeObject(latestproviderSnapShotByFundingPeriod);

                ProviderSnapshot latestProviderSnapshot = providerSnapshots.FirstOrDefault(x => x.FundingStreamCode == fundingStreamId);

                if (latestProviderSnapshot == null)
                {
                    continue;
                }

                ProviderVersionMetadata providerVersionMetadata = await GetProviderVersion(latestProviderSnapshot.ProviderVersionId);

                if (providerVersionMetadata != null && !providerVersionMetadata.IsValid)
                {
                    continue;
                }

                CurrentProviderVersionMetadata currentProviderVersionMetadata =
                    metadataForAllFundingStreams.SingleOrDefault(_ => _.FundingStreamId == fundingStreamId);

                var currentProviderSnapShotByFundingPeriodMeta = currentProviderVersionMetadata.FundingPeriod;
              
                bool isLatestSnapShotByFundingPeriodAvailable = latestproviderSnapShotByFundingPeriod.Where(_ => !currentProviderSnapShotByFundingPeriodMeta.Select(_ =>_.ProviderSnapshotId).Contains(_.ProviderSnapshotId)).ToList().Any();

                int latestProviderSnapshotId = latestProviderSnapshot.ProviderSnapshotId;
                int? currentProviderSnapshotId = currentProviderVersionMetadata?.ProviderSnapshotId;

                if (currentProviderSnapshotId != latestProviderSnapshotId || isLatestSnapShotByFundingPeriodAvailable)
                {
                    try
                    {
                        await _jobManagement.QueueJob(new JobCreateModel()
                        {
                            Trigger = new Trigger
                            {
                                EntityId = fundingStreamId,
                                EntityType = "FundingStream",
                                Message = "Tracking latest snapshot"
                            },
                            JobDefinitionId = JobConstants.DefinitionNames.TrackLatestJob,
                            CorrelationId = Guid.NewGuid().ToString(),
                            Properties = new Dictionary<string, string>
                            {
                                {"fundingstream-id", fundingStreamId},
                                {"providersnapshot-id", latestProviderSnapshotId.ToString()}                                
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"Failed to queue TrackLatestJob for funding stream - {fundingStreamId}";
                        _logger.Error(ex, errorMessage);
                        throw;
                    }
                }
            }
        }

        private async Task<IEnumerable<CurrentProviderVersionMetadata>> GetMetadataForAllFundingStreams()
        {
            IEnumerable<CurrentProviderVersion> currentProviderVersions =
                await _providerVersionMetadataPolicy.ExecuteAsync(() =>
                    _providerVersionMetadata.GetAllCurrentProviderVersions());

            return _mapper.Map<IEnumerable<CurrentProviderVersionMetadata>>(currentProviderVersions);
        }

        private async Task<ProviderVersionMetadata> GetProviderVersion(string providerVersionId)
        {
            ProviderVersionMetadata providerVersion =
                await _providerVersionMetadataPolicy.ExecuteAsync(() =>
                    _providerVersionMetadata.GetProviderVersionMetadata(providerVersionId));

            return providerVersion;
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

        private async Task<IEnumerable<ProviderSnapshot>> GetLatestProviderSnapshotsForAllFundingStreamsWithFundingPeriod()
        {
            ApiResponse<IEnumerable<ProviderSnapshot>> providerSnapshotResponse
                    = await _fundingDataZoneApiClientPolicy.ExecuteAsync(() =>
                        _fundingDataZoneApiClient.GetLatestProviderSnapshotsForAllFundingStreamsWithFundingPeriod());

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
    }
}
