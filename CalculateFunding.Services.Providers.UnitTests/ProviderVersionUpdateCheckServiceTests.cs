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
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Providers.MappingProfiles;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Polly;
using Serilog.Core;
using CalculateFunding.Common.JobManagement;
using FundingDataZoneProvider = CalculateFunding.Common.ApiClient.FundingDataZone.Models.Provider;
using CalculateFunding.Common.ApiClient.Jobs.Models;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderVersionUpdateCheckServiceTests
    {
        private ProviderVersionUpdateCheckService _service;
        private Mock<ICacheProvider> _cachProvider;
        private Mock<IPoliciesApiClient> _policiesApiClient;
        private Mock<IProviderVersionsMetadataRepository> _providerVersionsMetadataRepository;
        private Mock<IFundingDataZoneApiClient> _fundingDataZoneApiClient;
        private Mock<IJobManagement> _jobManagement;

        private IMapper _mapper;
        private IEnumerable<FundingStream> _fundingStreams;
        private string _fundingStreamOneId;
        private string _fundingPeriodId;
        private int _currentProviderSnapshotId;
        private int _latestProviderSnapshotId;

        private IEnumerable<FundingConfiguration> _fundingConfigurations;
        private IEnumerable<CurrentProviderVersion> _currentProviderVersions;
        private IEnumerable<ProviderSnapshot> _providerSnapshots;

        [TestInitialize]
        public void SetUp()
        {
            MapperConfiguration mappingConfig = new MapperConfiguration(c => { c.AddProfile<ProviderVersionsMappingProfile>(); });
            _mapper = mappingConfig.CreateMapper();

            _policiesApiClient = new Mock<IPoliciesApiClient>();
            _providerVersionsMetadataRepository = new Mock<IProviderVersionsMetadataRepository>();
            _fundingDataZoneApiClient = new Mock<IFundingDataZoneApiClient>();
            _jobManagement = new Mock<IJobManagement>();
            _cachProvider = new Mock<ICacheProvider>();

            ProvidersResiliencePolicies providersResiliencePolicies = new ProvidersResiliencePolicies
            {
                PoliciesApiClient = Policy.NoOpAsync(),
                ProviderVersionMetadataRepository = Policy.NoOpAsync(),
                FundingDataZoneApiClient = Policy.NoOpAsync(),
                SpecificationsApiClient = Policy.NoOpAsync(),
                CacheProvider = Policy.NoOpAsync()
            };

            _service = new ProviderVersionUpdateCheckService(
                _policiesApiClient.Object,
                _cachProvider.Object,
                Logger.None,
                providersResiliencePolicies,
                _providerVersionsMetadataRepository.Object,
                _fundingDataZoneApiClient.Object,
                _jobManagement.Object,
                _mapper
            );

            int previousProviderSnapshotId = NewRandomInteger();

            _fundingStreamOneId = NewRandomString();
            _fundingPeriodId = NewRandomString();
            _currentProviderSnapshotId = NewRandomInteger();
            _latestProviderSnapshotId = NewRandomInteger();

            _fundingStreams = new List<FundingStream>
            {
                new FundingStream
                {
                    Id = _fundingStreamOneId
                }
            };

            _fundingConfigurations = new List<FundingConfiguration>
            {
                new FundingConfiguration
                {
                    FundingStreamId = _fundingStreamOneId,
                    FundingPeriodId = _fundingPeriodId,
                    UpdateCoreProviderVersion = UpdateCoreProviderVersion.ToLatest
                }
            };

            _currentProviderVersions = new List<CurrentProviderVersion>
            {
                new CurrentProviderVersion
                {
                    Id = $"Current_{_fundingStreamOneId}",
                    ProviderSnapshotId = _currentProviderSnapshotId
                }
            };

            _providerSnapshots = new List<ProviderSnapshot>
            {
                new ProviderSnapshot
                {
                    TargetDate = DateTime.UtcNow.AddDays(1),
                    Version = 1,
                    ProviderSnapshotId = previousProviderSnapshotId,
                    FundingStreamCode = _fundingStreamOneId
                },
                new ProviderSnapshot
                {
                    TargetDate = DateTime.UtcNow.AddDays(2),
                    Version = 2,
                    ProviderSnapshotId = previousProviderSnapshotId,
                    FundingStreamCode = NewRandomString()
                },
                new ProviderSnapshot
                {
                    TargetDate = DateTime.UtcNow.AddDays(2),
                    Version = 3,
                    ProviderSnapshotId = _latestProviderSnapshotId,
                    FundingStreamCode = NewRandomString()
                }
            };
        }

        [TestMethod]
        public void CheckProviderVersionUpdate_GivenFundingStreamsNotFound_ThrowsError()
        {
            GivenErrorGetFundingStreams();

            Func<Task> invocation = WhenTheProviderVersionUpdateIsChecked;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be("Unable to retrieve funding streams");
        }

        [TestMethod]
		[DataRow(true)]
		[DataRow(false)]
        public async Task CheckProviderVersionUpdate_GivenCurrentProviderVersionNotLatest_QueuesJob(bool currentVersionExists)
        {
            ProviderSnapshot providerSnapshot = _providerSnapshots.Single(_ => _.Version == 1);
            string providerVersionId = $"{providerSnapshot.FundingStreamCode}-{providerSnapshot.TargetDate:yyyy}-{providerSnapshot.TargetDate:MM}-{providerSnapshot.TargetDate:dd}-{providerSnapshot.ProviderSnapshotId}";
            string currentProviderId = $"Current_{providerSnapshot.FundingStreamCode}";
            
            FundingDataZoneProvider expectedProviderOne = NewFundingDataZoneProvider();
            FundingDataZoneProvider expectedProviderTwo = NewFundingDataZoneProvider();

            GivenTheFundingStreams(_fundingStreams);
            if (currentVersionExists)
			{
				AndTheCurrentProviderVersions(_currentProviderVersions);
			}
            AndTheCurrentProviderVersionSaved(currentProviderId, providerVersionId, providerSnapshot.ProviderSnapshotId);
            AndTheFundingConfigurationsForTheFundingStreamId(_fundingConfigurations, _fundingStreamOneId);
            AndTheProviderSnapshotsForFundingStream(_providerSnapshots);
            AndTheFundingDataZoneProvidersForTheSnapshot(providerSnapshot.ProviderSnapshotId, expectedProviderOne, expectedProviderTwo);

            await WhenTheProviderVersionUpdateIsChecked();

            ThenQueuesJob(_fundingStreamOneId, providerSnapshot.ProviderSnapshotId.ToString());
        }

        [TestMethod]
        public async Task SkipsUpdatesIfTrackingDisabled()
        {
            GivenDisableTrackLatest(true);

            await WhenTheProviderVersionUpdateIsChecked();

            ThenDoesNotQueueJob();
        }

        [TestMethod]
        public async Task SkipsUpdatesForFundingStreamWithoutLatestProviderSnapshot()
        {
            IEnumerable<ProviderSnapshot> providerSnapshots = Array.Empty<ProviderSnapshot>();

            GivenTheFundingStreams(_fundingStreams);
            AndTheCurrentProviderVersions(_currentProviderVersions);
            AndTheFundingConfigurationsForTheFundingStreamId(_fundingConfigurations, _fundingStreamOneId);
            AndTheProviderSnapshotsForFundingStream(providerSnapshots);
            
            await WhenTheProviderVersionUpdateIsChecked();

            ThenDoesNotQueueJob();
        }

        private void ThenQueuesJob(string fundingStreamId, string providerSnapshotId) =>
            _jobManagement
                .Verify(_ => _.QueueJob(It.Is<JobCreateModel>(_ => _.Properties.ContainsKey("fundingstream-id") &&
                    _.Properties.ContainsKey("providersnapshot-id") && 
                    _.Properties["fundingstream-id"] == fundingStreamId && 
                    _.Properties["providersnapshot-id"] == providerSnapshotId)), Times.Once);

        private void ThenDoesNotQueueJob() =>
            _jobManagement
                .Verify(_ => _.QueueJob(It.IsAny<JobCreateModel>()), Times.Never);

        private void GivenErrorGetFundingStreams() =>
            _policiesApiClient
                .Setup(_ => _.GetFundingStreams())
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingStream>>(HttpStatusCode.BadRequest));

        private void GivenTheFundingStreams(IEnumerable<FundingStream> fundingStreams) =>
            _policiesApiClient
                .Setup(_ => _.GetFundingStreams())
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingStream>>(HttpStatusCode.OK, fundingStreams));

        private void GivenDisableTrackLatest(bool trackLatest)
        {
            _cachProvider
                .Setup(_ => _.GetAsync<bool>(CacheKeys.DisableTrackLatest, It.IsAny<JsonSerializerSettings>()))
                .ReturnsAsync(trackLatest);
        }

        private void AndTheFundingConfigurationsForTheFundingStreamId(IEnumerable<FundingConfiguration> fundingConfigurations,
            string fundingStreamId) =>
            _policiesApiClient
                .Setup(_ => _.GetFundingConfigurationsByFundingStreamId(fundingStreamId))
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingConfiguration>>(HttpStatusCode.OK, fundingConfigurations));

        private void AndTheCurrentProviderVersions(IEnumerable<CurrentProviderVersion> currentProviderVersions) =>
            _providerVersionsMetadataRepository
                .Setup(_ => _.GetAllCurrentProviderVersions())
                .ReturnsAsync(new List<CurrentProviderVersion>(currentProviderVersions));

        private void AndTheCurrentProviderVersionSaved(string id, string providerVersionId, int providerSnapshotId) =>
            _providerVersionsMetadataRepository
                .Setup(_ => _.UpsertCurrentProviderVersion(It.Is<CurrentProviderVersion>(cpv => cpv.Id == id && 
                                                                                                cpv.ProviderVersionId == providerVersionId &&
                                                                                                cpv.ProviderSnapshotId == providerSnapshotId)))
                .ReturnsAsync(HttpStatusCode.OK);

        private void AndTheProviderSnapshotsForFundingStream(IEnumerable<ProviderSnapshot> providerSnapshots) =>
            _fundingDataZoneApiClient
                .Setup(_ => _.GetLatestProviderSnapshotsForAllFundingStreams())
                .ReturnsAsync(new ApiResponse<IEnumerable<ProviderSnapshot>>(HttpStatusCode.OK, providerSnapshots));

        private void AndTheFundingDataZoneProvidersForTheSnapshot(int providerSnapshotId,
            params FundingDataZoneProvider[] providers)
            => _fundingDataZoneApiClient.Setup(_ => _.GetProvidersInSnapshot(providerSnapshotId))
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingDataZoneProvider>>(HttpStatusCode.OK, providers));

        private async Task WhenTheProviderVersionUpdateIsChecked() =>
            await _service.CheckProviderVersionUpdate();

        private FundingDataZoneProvider NewFundingDataZoneProvider(Action<FundingDataZoneProviderBuilder> setUp = null)
        {
            FundingDataZoneProviderBuilder fundingDataZoneProviderBuilder = new FundingDataZoneProviderBuilder();

            setUp?.Invoke(fundingDataZoneProviderBuilder);

            return fundingDataZoneProviderBuilder.Build();
        }

        private static string NewRandomString() => new RandomString();

        private static int NewRandomInteger() => new RandomNumberBetween(0, int.MaxValue);
    }
}