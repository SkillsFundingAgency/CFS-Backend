using AutoFixture;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
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
        private const int FundingGroupVersionId = 1;
        private int _channelId = new RandomNumberBetween(1, 10);
        private Mock<IReleaseManagementRepository> _releaseManagementRepository;
        private Mock<IReleaseToChannelSqlMappingContext> _context;
        private FundingGroupDataPersistenceService _service;
        private Fixture _fixture;
        private IEnumerable<GeneratedPublishedFunding> _fundingGroupData;

        [TestInitialize]
        public void Initialise()
        {
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();
            _context = new Mock<IReleaseToChannelSqlMappingContext>();
            _fixture = new Fixture();
            _fundingGroupData = _fixture.CreateMany<GeneratedPublishedFunding>();
            SetUpRepo();

            _service = new FundingGroupDataPersistenceService(_releaseManagementRepository.Object, _context.Object);
        }

        [TestMethod]
        public async Task ReleasesFundingGroups_Successfully()
        {
            GivenContext();

            await _service.ReleaseFundingGroupData(_fundingGroupData, _channelId);

            _releaseManagementRepository.Verify(
                _ => _.CreateFundingGroupVersionUsingAmbientTransaction(
                    It.Is<FundingGroupVersion>(f => f.ChannelId == _channelId)),
                        Times.Exactly(_fundingGroupData.Count()));

            _releaseManagementRepository.Verify(
                _ => _.CreateFundingGroupVariationReasonUsingAmbientTransaction(
                    It.Is<FundingGroupVersionVariationReason>(f => f.FundingGroupVersionId == FundingGroupVersionId)),
                        Times.Exactly(_fundingGroupData.SelectMany(s => s.PublishedFundingVersion.VariationReasons).Count()));
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
                .ReturnsAsync(new FundingGroupVersion { FundingGroupVersionId = FundingGroupVersionId });

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
            _releaseManagementRepository.Setup(_ => _.GetGroupingReasons())
                .ReturnsAsync(groupingReasons);
        }

        private void SetupFundingStreams()
        {
            IEnumerable<string> usedFundingStreams = _fundingGroupData.Select(s => s.PublishedFundingVersion.FundingStreamId);
            List<Publishing.FundingManagement.SqlModels.FundingStream> fundingStreams = new List<Publishing.FundingManagement.SqlModels.FundingStream>();
            int id = 1;
            foreach (string item in usedFundingStreams)
            {
                fundingStreams.Add(new Publishing.FundingManagement.SqlModels.FundingStream
                {
                    FundingStreamId = id++,
                    FundingStreamCode = item,
                    FundingStreamName = item
                });
            }
            _releaseManagementRepository.Setup(_ => _.GetFundingStreams())
                .ReturnsAsync(fundingStreams);
        }

        private void SetupFundingPeriods()
        {
            IEnumerable<string> usedFundingPeriods = _fundingGroupData.Select(s => s.PublishedFundingVersion.FundingPeriod.Id);
            List<Publishing.FundingManagement.SqlModels.FundingPeriod> fundingPeriods = new List<Publishing.FundingManagement.SqlModels.FundingPeriod>();
            int id = 1;
            foreach (string item in usedFundingPeriods)
            {
                fundingPeriods.Add(new Publishing.FundingManagement.SqlModels.FundingPeriod
                {
                    FundingPeriodId = id++,
                    FundingPeriodCode = item,
                    FundingPeriodName = item
                });
            }
            _releaseManagementRepository.Setup(_ => _.GetFundingPeriods())
                .ReturnsAsync(fundingPeriods);
        }

        private void SetupVariationReasons()
        {
            IEnumerable<CalculateFunding.Models.Publishing.VariationReason> usedVariationReasons = _fundingGroupData.SelectMany(s => s.PublishedFundingVersion.VariationReasons).Distinct();
            List<Publishing.FundingManagement.SqlModels.VariationReason> variationReasons = new List<Publishing.FundingManagement.SqlModels.VariationReason>();
            int id = 0;
            foreach (CalculateFunding.Models.Publishing.VariationReason item in usedVariationReasons)
            {
                variationReasons.Add(new Publishing.FundingManagement.SqlModels.VariationReason
                {
                    VariationReasonId = id++,
                    VariationReasonCode = item.ToString(),
                    VariationReasonName = item.ToString()
                });
            }
            _releaseManagementRepository.Setup(_ => _.GetVariationReasons())
                .ReturnsAsync(variationReasons);
        }

        private void GivenContext()
        {
            Dictionary<OrganisationGroupResult, int> fundingGroups = new Dictionary<OrganisationGroupResult, int>();
            int id = 0;
            foreach (OrganisationGroupResult item in _fundingGroupData.Select(s => s.OrganisationGroupResult))
            {
                fundingGroups.Add(item, id++);
            }
            _context.SetupGet(s => s.FundingGroups)
                .Returns(fundingGroups);

            _context.SetupGet(s => s.FundingGroupVersions)
                .Returns(new Dictionary<int, Dictionary<string, FundingGroupVersion>>());
        }

        private void GivenContextWithMissingFundingGroup()
        {
            Dictionary<OrganisationGroupResult, int> fundingGroups = new Dictionary<OrganisationGroupResult, int>();
            int id = 0;
            List<OrganisationGroupResult> organisationGroupResults = _fundingGroupData.Select(s => s.OrganisationGroupResult).ToList();
            organisationGroupResults.RemoveAt(0);
            foreach (OrganisationGroupResult item in organisationGroupResults)
            {
                fundingGroups.Add(item, id++);
            }
            _context.SetupGet(s => s.FundingGroups)
                .Returns(fundingGroups);
        }
    }
}
