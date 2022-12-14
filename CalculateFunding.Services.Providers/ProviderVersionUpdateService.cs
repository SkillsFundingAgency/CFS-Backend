using AutoMapper;
using CalculateFunding.Common.ApiClient.FundingDataZone;
using CalculateFunding.Common.ApiClient.FundingDataZone.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Providers;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Providers
{
    public class ProviderVersionUpdateService : JobProcessingService, IProviderVersionUpdateService
    {
        private readonly IPublishingJobClashCheck _publishingJobClashCheck;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly IProviderSnapshotPersistService _providerSnapshotPersistService;
        private readonly IProviderVersionsMetadataRepository _providerVersionMetadata;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IFundingDataZoneApiClient _fundingDataZoneApiClient;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        private readonly AsyncPolicy _policiesApiClientPolicy;
        private readonly AsyncPolicy _providerVersionMetadataPolicy;
        private readonly AsyncPolicy _specificationsApiClientPolicy;
        private readonly AsyncPolicy _fundingDataZoneApiClientPolicy;

        public ProviderVersionUpdateService(IPoliciesApiClient policiesApiClient,
            ILogger logger,
            IProvidersResiliencePolicies resiliencePolicies,
            IProviderVersionsMetadataRepository providerVersionMetadata,
            ISpecificationsApiClient specificationsApiClient,
            IPublishingJobClashCheck publishingJobClashCheck,
            IProviderSnapshotPersistService providerSnapshotPersistService,
            IFundingDataZoneApiClient fundingDataZoneApiClient,
            IMapper mapper,
            IJobManagement jobManagement) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies.ProviderVersionMetadataRepository, nameof(resiliencePolicies.ProviderVersionMetadataRepository));
            Guard.ArgumentNotNull(resiliencePolicies.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies.FundingDataZoneApiClient, nameof(resiliencePolicies.FundingDataZoneApiClient));
            Guard.ArgumentNotNull(providerVersionMetadata, nameof(providerVersionMetadata));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(publishingJobClashCheck, nameof(publishingJobClashCheck));
            Guard.ArgumentNotNull(providerSnapshotPersistService, nameof(providerSnapshotPersistService));
            Guard.ArgumentNotNull(fundingDataZoneApiClient, nameof(fundingDataZoneApiClient));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _policiesApiClient = policiesApiClient;
            _providerVersionMetadata = providerVersionMetadata;
            _specificationsApiClient = specificationsApiClient;
            _publishingJobClashCheck = publishingJobClashCheck;
            _fundingDataZoneApiClient = fundingDataZoneApiClient;
            _mapper = mapper;
            _logger = logger;
            _policiesApiClientPolicy = resiliencePolicies.PoliciesApiClient;
            _providerVersionMetadataPolicy = resiliencePolicies.ProviderVersionMetadataRepository;
            _specificationsApiClientPolicy = resiliencePolicies.SpecificationsApiClient;
            _fundingDataZoneApiClientPolicy = resiliencePolicies.FundingDataZoneApiClient;
            _providerSnapshotPersistService = providerSnapshotPersistService;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth providerVersionHealth = await ((IHealthChecker)_providerVersionMetadata).IsHealthOk();
            ServiceHealth snapshotPersistenceHealth = await ((IHealthChecker)_providerSnapshotPersistService).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderVersionUpdateService)
            };

            health.Dependencies.AddRange(providerVersionHealth.Dependencies);
            health.Dependencies.AddRange(snapshotPersistenceHealth.Dependencies);

            return health;
        }

        public override async Task Process(Message message)
        {
            string latestProviderSnapshotId = message.GetUserProperty<string>("providersnapshot-id");
            string fundingStreamId = message.GetUserProperty<string>("fundingstream-id");

            Guard.ArgumentNotNull(latestProviderSnapshotId, nameof(latestProviderSnapshotId));
            Guard.ArgumentNotNull(fundingStreamId, nameof(fundingStreamId));

            IEnumerable<ProviderSnapshot> providerSnapshots = await GetLatestProviderSnapshots();           
            if (providerSnapshots.IsNullOrEmpty())
            {
                return;
            }
            
            ProviderSnapshot latestProviderSnapshot =  providerSnapshots.FirstOrDefault(_ => _.ProviderSnapshotId == Convert.ToInt32(latestProviderSnapshotId));

            if (latestProviderSnapshot == null)
            {
                return;
            }

            IEnumerable<ProviderSnapshot> SnapshotIdWithFundingPeriod = await GetLatestProviderSnapshotsForAllFundingStreamsWithFundingPeriod();

            if (SnapshotIdWithFundingPeriod.IsNullOrEmpty())
            {
                return;
            }

            CurrentProviderVersionMetadata currentProviderSnapshot = await GetMetadataForFundingStream(latestProviderSnapshot.FundingStreamCode);
    
            var latestproviderSnapShotByFundingPeriod = SnapshotIdWithFundingPeriod.Where(_ => _.FundingStreamCode == fundingStreamId)
                .Select(_ => new ProviderSnapShotByFundingPeriod() { FundingPeriodName = _.FundingPeriodName, ProviderSnapshotId = _.ProviderSnapshotId, ProviderVersionId = _.ProviderVersionId }).ToList();
            
            bool isLatestSnapShotByFundingPeriodAvailable = (currentProviderSnapshot != null)
                  ? latestproviderSnapShotByFundingPeriod.Where(_ => !currentProviderSnapshot.FundingPeriod.Select(_ => _.ProviderSnapshotId).Contains(_.ProviderSnapshotId)).ToList().Any()
                  : true;

            if (currentProviderSnapshot?.ProviderSnapshotId != latestProviderSnapshot.ProviderSnapshotId || isLatestSnapShotByFundingPeriodAvailable)
            {
                bool success = await _providerSnapshotPersistService.PersistSnapshot(latestProviderSnapshot);

                if (success)
                {
                    HttpStatusCode httpStatusCode = await _providerVersionMetadataPolicy.ExecuteAsync(() =>
                           _providerVersionMetadata.UpsertCurrentProviderVersion(
                               new CurrentProviderVersion
                               {
                                   Id = $"Current_{fundingStreamId}",                                  
                                   ProviderSnapshotId = latestProviderSnapshot.ProviderSnapshotId,
                                   ProviderVersionId = latestProviderSnapshot.ProviderVersionId,
                                   FundingPeriod = latestproviderSnapShotByFundingPeriod
                               }));

                    success = httpStatusCode.IsSuccess();
                }

                if (!success)
                {
                    string errorMessage = $"Unable to set current provider for funding stream with ID: {latestProviderSnapshot.FundingStreamCode}";
                    _logger.Error(errorMessage);
                    throw new RetriableException(errorMessage);
                }

                _logger.Information($"Triggering Update Specification Provider Version for funding stream ID: {latestProviderSnapshot.FundingStreamCode}. " +
                               $"Current provider snapshot ID: {latestProviderSnapshot.ProviderSnapshotId}");
            }

            IEnumerable<SpecificationSummary> specificationSummaries = await GetSpecificationsWithProviderVersionUpdatesAsUseLatest(latestProviderSnapshot.FundingStreamCode, latestProviderSnapshot.ProviderSnapshotId);

            if (specificationSummaries.AnyWithNullCheck())
            {
                await EditSpecifications(latestProviderSnapshot, specificationSummaries, latestproviderSnapShotByFundingPeriod);
            }
        }

        private async Task<CurrentProviderVersionMetadata> GetMetadataForFundingStream(string fundingStreamId)
        {
            CurrentProviderVersion currentProviderVersion =
                await _providerVersionMetadataPolicy.ExecuteAsync(() =>
                    _providerVersionMetadata.GetCurrentProviderVersion(fundingStreamId));

            return _mapper.Map<CurrentProviderVersionMetadata>(currentProviderVersion);
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

        private async Task<IEnumerable<SpecificationSummary>> GetSpecificationsWithProviderVersionUpdatesAsUseLatest(string fundingStreamId, int? providerSnapShotId)
        {
            ApiResponse<IEnumerable<SpecificationSummary>> specificationsWithProviderVersionUpdateResponse =
                await _specificationsApiClientPolicy.ExecuteAsync(() =>
                    _specificationsApiClient.GetSpecificationsWithProviderVersionUpdatesAsUseLatest());

            if (specificationsWithProviderVersionUpdateResponse.StatusCode == HttpStatusCode.NotFound)
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

            return specificationsWithProviderVersionUpdateResponse.Content.Where(_ => _.FundingStreams.Any(fs => fs.Id == fundingStreamId) /*&& _.ProviderSnapshotId != providerSnapShotId*/);
        }

        private async Task EditSpecifications(
            ProviderSnapshot latestProviderSnapshot,
            IEnumerable<SpecificationSummary> specificationSummaries,
            List<ProviderSnapShotByFundingPeriod> latestproviderSnapShotByFundingPeriod)
        {
            IEnumerable<FundingConfiguration> fundingStreamConfigurations = await GetFundingConfigurationsByFundingStreamId(latestProviderSnapshot.FundingStreamCode);

            if (!fundingStreamConfigurations.AnyWithNullCheck())
            {
                return;
            }

            IEnumerable<FundingConfiguration> periodsWithUpdatesEnabled = fundingStreamConfigurations.Where(_ => _.UpdateCoreProviderVersion == UpdateCoreProviderVersion.ToLatest);

            if (!periodsWithUpdatesEnabled.Any())
            {
                return;
            }

            foreach (SpecificationSummary specificationSummary in specificationSummaries)
            {
                string fundingStreamId = specificationSummary.FundingStreams.FirstOrDefault().Id;

                if (!latestProviderSnapshot.FundingStreamCode.Equals(fundingStreamId, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Information($"Skipping Update Specification Provider Version for funding stream ID: {fundingStreamId}. " +
                                       "Latest provider snapshot IDs does not contain item for above funding stream ID.");

                    continue;
                }

                foreach(ProviderSnapShotByFundingPeriod latestproviderSnapShotId in latestproviderSnapShotByFundingPeriod)
                {
                    if (periodsWithUpdatesEnabled.Any(_ =>
                    _.FundingPeriodId == specificationSummary.FundingPeriod.Id &&
                    _.FundingStreamId == fundingStreamId) && specificationSummary.ProviderSnapshotId != latestproviderSnapShotId.ProviderSnapshotId 
                     && specificationSummary.FundingPeriod.Id == latestproviderSnapShotId.FundingPeriodName)
                    {
                        if (await _publishingJobClashCheck.PublishingJobsClashWithFundingStreamCoreProviderUpdate(specificationSummary.GetSpecificationId()))
                        {
                            _logger.Information($"Skipping Update Specification Provider Version for funding stream ID: {fundingStreamId}. " +
                                           $"Latest provider snapshot ID: {latestproviderSnapShotId.ProviderSnapshotId} as detected publishing jobs running for specifications using an earlier snapshot of this provider data");

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
                                        ProviderVersionId = latestproviderSnapShotId.ProviderVersionId,
                                        ProviderSnapshotId = latestproviderSnapShotId.ProviderSnapshotId,
                                        CoreProviderVersionUpdates = CoreProviderVersionUpdates.UseLatest
                                    }));
                    }
                }              
            }
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
    }
}
