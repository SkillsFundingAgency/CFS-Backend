using AutoMapper;
using CalculateFunding.Common.ApiClient.FundingDataZone;
using CalculateFunding.Common.ApiClient.FundingDataZone.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Providers;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Providers.MappingProfiles;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderVersionUpdateCheckServiceTests
    {
        private ProviderVersionUpdateCheckService _service;
        private Mock<IPoliciesApiClient> _policiesApiClient;
        private Mock<IProviderVersionsMetadataRepository> _providerVersionsMetadataRepository;
        private Mock<IFundingDataZoneApiClient> _fundingDataZoneApiClient;
        private Mock<ISpecificationsApiClient> _specificationsApiClient;

        private IMapper _mapper;
        private IEnumerable<FundingStream> _fundingStreams;
        private string _fundingStreamOneId;
        private string _fundingPeriodId;
        private int _currentProviderSnapshotId;
        private int _latestProviderSnapshotId;
        private string _specificationId;
        private string _specificationName;
        private string _specificationDescription;
        private string _specificationProviderVersionId;

        private IEnumerable<FundingConfiguration> _fundingConfigurations;
        private IEnumerable<CurrentProviderVersion> _currentProviderVersions;
        private IEnumerable<ProviderSnapshot> _providerSnapshots;
        private IEnumerable<SpecificationSummary> _specificationSummaries;

        [TestInitialize]
        public void SetUp()
        {
            int _previousProviderSnapshotId;

            MapperConfiguration mappingConfig = new MapperConfiguration(c => { c.AddProfile<ProviderVersionsMappingProfile>(); });
            _mapper = mappingConfig.CreateMapper();

            _policiesApiClient = new Mock<IPoliciesApiClient>();
            _providerVersionsMetadataRepository = new Mock<IProviderVersionsMetadataRepository>();
            _fundingDataZoneApiClient = new Mock<IFundingDataZoneApiClient>();
            _specificationsApiClient = new Mock<ISpecificationsApiClient>();

            _service = new ProviderVersionUpdateCheckService(
                policiesApiClient: _policiesApiClient.Object,
                logger: Logger.None,
                resiliencePolicies: new ProvidersResiliencePolicies
                {
                    PoliciesApiClient = Policy.NoOpAsync(),
                    ProviderVersionMetadataRepository = Policy.NoOpAsync(),
                    FundingDataZoneApiClient = Policy.NoOpAsync(),
                    SpecificationsApiClient = Policy.NoOpAsync()
                },
                providerVersionMetadata: _providerVersionsMetadataRepository.Object,
                fundingDataZoneApiClient: _fundingDataZoneApiClient.Object,
                specificationsApiClient: _specificationsApiClient.Object,
                mapper: _mapper
            );

            _fundingStreamOneId = NewRandomString();
            _fundingPeriodId = NewRandomString();
            _currentProviderSnapshotId = NewRandomInteger();
            _latestProviderSnapshotId = NewRandomInteger();
            _previousProviderSnapshotId = NewRandomInteger();
            _specificationName = NewRandomString();
            _specificationDescription = NewRandomString();
            _specificationProviderVersionId = NewRandomString();
            _specificationId = NewRandomString();

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
                    ProviderSnapshotId = _previousProviderSnapshotId
                },
                new ProviderSnapshot
                {
                    TargetDate = DateTime.UtcNow.AddDays(2),
                    Version = 2,
                    ProviderSnapshotId = _previousProviderSnapshotId
                },
                new ProviderSnapshot
                {
                    TargetDate = DateTime.UtcNow.AddDays(2),
                    Version = 3,
                    ProviderSnapshotId = _latestProviderSnapshotId
                }
            };

            _specificationSummaries = new List<SpecificationSummary>
            {
                new SpecificationSummary
                {
                    Id = _specificationId,
                    FundingStreams = new List<FundingStream>
                    {
                        new FundingStream
                        {
                            Id = _fundingStreamOneId
                        }
                    },
                    FundingPeriod = new Common.Models.Reference
                    {
                        Id = _fundingPeriodId
                    },
                    Name = _specificationName,
                    Description = _specificationDescription,
                    ProviderVersionId = _specificationProviderVersionId
                }
            };
        }

        [TestMethod]
        public async Task CheckProviderVersionUpdate_GivenFundingStreamsNotFound_ThrowsError()
        {
            GivenErrorGetFundingStreams();

            Func<Task> invocation = () => WhenCheckProviderVersionUpdate();

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be("Unable to retrieve funding streams");
        }

        [TestMethod]
        public async Task CheckProviderVersionUpdate_GivenGetSpecificationsWithProviderVersionUpdatesAsUseLatestNotFound_ThrowsError()
        {
            GivenGetFundingStreams();
            AndGetAllCurrentProviderVersions();
            AndGetFundingConfigurationsByFundingStreamId();
            AndGetProviderSnapshotsForFundingStream();
            AndErrorGetSpecificationsWithProviderVersionUpdatesAsUseLatest();

            Func<Task> invocation = () => WhenCheckProviderVersionUpdate();

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be("Unable to retrieve specification with provider version updates as use latest");
        }

        [TestMethod]
        public async Task CheckProviderVersionUpdate_GivenGetSpecificationsWithProviderVersionUpdatesAsUseLatest_UpdatesSpecification()
        {
            GivenGetFundingStreams();
            AndGetAllCurrentProviderVersions();
            AndGetFundingConfigurationsByFundingStreamId();
            AndGetProviderSnapshotsForFundingStream();
            AndGetSpecificationsWithProviderVersionUpdatesAsUseLatest();

            await WhenCheckProviderVersionUpdate();

            ThenUpdatesSpecification();
        }

        private void ThenUpdatesSpecification() =>
            _specificationsApiClient
                .Verify(_ => _.UpdateSpecification(_specificationId, It.IsAny<EditSpecificationModel>()), Times.Once); 

        private void GivenErrorGetFundingStreams() =>
            _policiesApiClient
                .Setup(_ => _.GetFundingStreams())
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingStream>>(System.Net.HttpStatusCode.BadRequest));

        private void GivenGetFundingStreams() =>
            _policiesApiClient
                .Setup(_ => _.GetFundingStreams())
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingStream>>(System.Net.HttpStatusCode.OK, _fundingStreams));

        private void AndGetFundingConfigurationsByFundingStreamId() =>
            _policiesApiClient
                .Setup(_ => _.GetFundingConfigurationsByFundingStreamId(_fundingStreamOneId))
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingConfiguration>>(System.Net.HttpStatusCode.OK, _fundingConfigurations));

        private void AndErrorGetFundingConfigurationsByFundingStreamId() =>
            _policiesApiClient
                .Setup(_ => _.GetFundingConfigurationsByFundingStreamId(_fundingStreamOneId))
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingConfiguration>>(System.Net.HttpStatusCode.BadRequest));

        private void AndGetAllCurrentProviderVersions() =>
            _providerVersionsMetadataRepository
                .Setup(_ => _.GetAllCurrentProviderVersions())
                .ReturnsAsync(new List<CurrentProviderVersion>(_currentProviderVersions));

        private void AndGetProviderSnapshotsForFundingStream() =>
            _fundingDataZoneApiClient
                .Setup(_ => _.GetProviderSnapshotsForFundingStream(_fundingStreamOneId))
                .ReturnsAsync(new ApiResponse<IEnumerable<ProviderSnapshot>>(System.Net.HttpStatusCode.OK, _providerSnapshots));

        private void AndErrorGetProviderSnapshotsForFundingStream() =>
            _fundingDataZoneApiClient
                .Setup(_ => _.GetProviderSnapshotsForFundingStream(_fundingStreamOneId))
                .ReturnsAsync(new ApiResponse<IEnumerable<ProviderSnapshot>>(System.Net.HttpStatusCode.BadRequest));

        private void AndGetSpecificationsWithProviderVersionUpdatesAsUseLatest() =>
            _specificationsApiClient
                .Setup(_ => _.GetSpecificationsWithProviderVersionUpdatesAsUseLatest())
                .ReturnsAsync(new ApiResponse<IEnumerable<SpecificationSummary>>(System.Net.HttpStatusCode.OK, _specificationSummaries));

        private void AndErrorGetSpecificationsWithProviderVersionUpdatesAsUseLatest() =>
            _specificationsApiClient
                .Setup(_ => _.GetSpecificationsWithProviderVersionUpdatesAsUseLatest())
                .ReturnsAsync(new ApiResponse<IEnumerable<SpecificationSummary>>(System.Net.HttpStatusCode.BadRequest));

        private async Task WhenCheckProviderVersionUpdate() =>
            await _service.CheckProviderVersionUpdate();

        private static string NewRandomString() => new RandomString();
        private static int NewRandomInteger() => new RandomNumberBetween(0, 100);


    }
}
