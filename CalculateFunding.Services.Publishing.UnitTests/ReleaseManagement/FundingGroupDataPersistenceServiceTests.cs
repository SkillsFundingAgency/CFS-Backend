using AutoFixture;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class FundingGroupDataPersistenceServiceTests
    {
        private Guid _fundingGroupVersionId;
        private Mock<IUniqueIdentifierProvider> _fundingGroupIdentifierGenerator;
        private Mock<IUniqueIdentifierProvider> _fundingGroupVersionIdentifierGenerator;
        private readonly int _channelId = new RandomNumberBetween(1, 10);
        private Mock<IReleaseManagementRepository> _releaseManagementRepository;
        private Mock<IReleaseToChannelSqlMappingContext> _context;
        private FundingGroupDataPersistenceService _service;
        private Fixture _fixture;
        private IEnumerable<GeneratedPublishedFunding> _fundingGroupData;
        private List<Guid> _fundingGroupDataIds;

        [TestInitialize]
        public void Initialise()
        {
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();
            _context = new Mock<IReleaseToChannelSqlMappingContext>();
            _fixture = new Fixture();
            _fundingGroupData = _fixture.CreateMany<GeneratedPublishedFunding>();

            _fundingGroupDataIds = new List<Guid>();
            for (int i = 0; i < _fundingGroupData.Count(); i++)
            {
                _fundingGroupDataIds.Add(Guid.NewGuid());
            }

            _fundingGroupVersionId = Guid.NewGuid();

            _fundingGroupIdentifierGenerator = new Mock<IUniqueIdentifierProvider>();
            _fundingGroupVersionIdentifierGenerator = new Mock<IUniqueIdentifierProvider>();


            SetUpRepo();

            _service = new FundingGroupDataPersistenceService(
                _releaseManagementRepository.Object,
                _context.Object,
                _fundingGroupIdentifierGenerator.Object,
                _fundingGroupVersionIdentifierGenerator.Object
                );
        }

        [TestMethod]
        public async Task ReleasesFundingGroupVersions_Successfully()
        {
            GivenContext();

            await _service.ReleaseFundingGroupData(_fundingGroupData, _channelId);

            _releaseManagementRepository.Verify(
                _ => _.BulkCreateFundingGroupVersionsUsingAmbientTransaction(
                    It.Is<IEnumerable<FundingGroupVersion>>(f => f.Count() == _fundingGroupData.Count())),
                Times.Once);

            _releaseManagementRepository.Verify(
                _ => _.BulkCreateFundingGroupVersionVariationReasonsUsingAmbientTransaction(
                    It.Is<IEnumerable<FundingGroupVersionVariationReason>>(f => f.Count() == _fundingGroupData.SelectMany(s => s.PublishedFundingVersion.VariationReasons).Count())),
                        Times.Once());
        }

        [TestMethod]
        public void WhenFundingGroupMissingFromContext_Throws()
        {
            GivenContextWithMissingFundingGroup();

            Func<Task> result = async () => await _service.ReleaseFundingGroupData(_fundingGroupData, _channelId);

            result
                .Should()
                .ThrowExactly<KeyNotFoundException>();
        }

        private void SetUpRepo()
        {
            SetupGroupingReasons();
            SetupFundingStreams();
            SetupFundingPeriods();
            SetupVariationReasons();

            _releaseManagementRepository.Setup(_ => _.CreateFundingGroupVersionUsingAmbientTransaction(It.IsAny<FundingGroupVersion>()))
                .ReturnsAsync(new FundingGroupVersion { FundingGroupVersionId = _fundingGroupVersionId });

            _releaseManagementRepository.Setup(_ => _.CreateFundingGroupProviderUsingAmbientTransaction(It.IsAny<FundingGroupProvider>()))
                .ReturnsAsync(new FundingGroupProvider());
        }

        private void SetupGroupingReasons()
        {
            IEnumerable<CalculateFunding.Models.Publishing.GroupingReason> usedGroupingReasons =
                _fundingGroupData.Select(s => s.PublishedFundingVersion.GroupingReason)
                .Distinct();

            List<Publishing.FundingManagement.SqlModels.GroupingReason> groupingReasons = new List<Publishing.FundingManagement.SqlModels.GroupingReason>();
            int id = 1;
            foreach (CalculateFunding.Models.Publishing.GroupingReason item in usedGroupingReasons)
            {
                groupingReasons.Add(new Publishing.FundingManagement.SqlModels.GroupingReason
                {
                    GroupingReasonId = id++,
                    GroupingReasonCode = item.ToString(),
                    GroupingReasonName = item.ToString()
                });
            }
            _releaseManagementRepository.Setup(_ => _.GetGroupingReasonsUsingAmbientTransaction())
                .ReturnsAsync(groupingReasons);
        }

        private void SetupFundingStreams()
        {
            IEnumerable<string> usedFundingStreams = _fundingGroupData.Select(s => s.PublishedFundingVersion.FundingStreamId);
            List<FundingStream> fundingStreams = new List<FundingStream>();
            int id = 1;
            foreach (string item in usedFundingStreams)
            {
                fundingStreams.Add(new FundingStream
                {
                    FundingStreamId = id++,
                    FundingStreamCode = item,
                    FundingStreamName = item
                });
            }
            _releaseManagementRepository.Setup(_ => _.GetFundingStreamsUsingAmbientTransaction())
                .ReturnsAsync(fundingStreams);
        }

        private void SetupFundingPeriods()
        {
            IEnumerable<string> usedFundingPeriods = _fundingGroupData.Select(s => s.PublishedFundingVersion.FundingPeriod.Id);
            List<FundingPeriod> fundingPeriods = new List<FundingPeriod>();
            int id = 1;
            foreach (string item in usedFundingPeriods)
            {
                fundingPeriods.Add(new FundingPeriod
                {
                    FundingPeriodId = id++,
                    FundingPeriodCode = item,
                    FundingPeriodName = item
                });
            }
            _releaseManagementRepository.Setup(_ => _.GetFundingPeriodsUsingAmbientTransaction())
                .ReturnsAsync(fundingPeriods);
        }

        private void SetupVariationReasons()
        {
            IEnumerable<CalculateFunding.Models.Publishing.VariationReason> usedVariationReasons = _fundingGroupData.SelectMany(s => s.PublishedFundingVersion.VariationReasons).Distinct();
            List<VariationReason> variationReasons = new List<VariationReason>();
            int id = 0;
            foreach (CalculateFunding.Models.Publishing.VariationReason item in usedVariationReasons)
            {
                variationReasons.Add(new VariationReason
                {
                    VariationReasonId = id++,
                    VariationReasonCode = item.ToString(),
                    VariationReasonName = item.ToString()
                });
            }
            _releaseManagementRepository.Setup(_ => _.GetVariationReasonsUsingAmbientTransaction())
                .ReturnsAsync(variationReasons);
        }

        private void GivenContext()
        {
            Dictionary<int, Dictionary<OrganisationGroupResult, Guid>> fundingGroups = new Dictionary<int, Dictionary<OrganisationGroupResult, Guid>>();
            int i = 0;

            fundingGroups.Add(_channelId, new Dictionary<OrganisationGroupResult, Guid>());

            foreach (OrganisationGroupResult item in _fundingGroupData.Select(s => s.OrganisationGroupResult))
            {
                fundingGroups[_channelId].Add(item, _fundingGroupDataIds[i++]);
            }
            _context.SetupGet(s => s.FundingGroups)
                .Returns(fundingGroups);

            _context.SetupGet(s => s.FundingGroupVersions)
                .Returns(new Dictionary<int, Dictionary<string, FundingGroupVersion>>());
        }

        private void GivenContextWithMissingFundingGroup()
        {
            Dictionary<int, Dictionary<OrganisationGroupResult, Guid>> fundingGroups = new Dictionary<int, Dictionary<OrganisationGroupResult, Guid>>();

            fundingGroups.Add(_channelId, new Dictionary<OrganisationGroupResult, Guid>());

            List<OrganisationGroupResult> organisationGroupResults = _fundingGroupData.Select(s => s.OrganisationGroupResult).ToList();
            organisationGroupResults.RemoveAt(0);
            int i = 1;
            foreach (OrganisationGroupResult item in organisationGroupResults)
            {
                fundingGroups[_channelId].Add(item, _fundingGroupDataIds[i++]);
            }
            _context.SetupGet(s => s.FundingGroups)
                .Returns(fundingGroups);
        }
    }
}
