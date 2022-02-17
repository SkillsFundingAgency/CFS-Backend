using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class FundingGroupServiceTests
    {
        private const string Identifier1 = "1";
        private const string Identifier2 = "2";
        private RandomString _specificationId;
        private RandomNumberBetween _channelId;
        private Mock<IReleaseManagementRepository> _releaseManagementRepository;
        private Mock<IReleaseToChannelSqlMappingContext> _context;
        private Mock<ILogger> _logger;
        private FundingGroup _fundingGroupOne;
        private FundingGroup _fundingGroupTwo;
        private List<OrganisationGroupResult> _results;
        private FundingGroupService _service;

        [TestInitialize]
        public void Initialise()
        {
            _specificationId = new RandomString();
            _channelId = new RandomNumberBetween(1, 10);
            _logger = new Mock<ILogger>();
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();
            _releaseManagementRepository.Setup(r => r.CreateFundingGroupUsingAmbientTransaction(It.IsAny<FundingGroup>())).ReturnsAsync(new FundingGroup());
            _context = new Mock<IReleaseToChannelSqlMappingContext>();
            _context.SetupGet(s => s.FundingGroups)
                .Returns(new Dictionary<OrganisationGroupResult, int>());
            _fundingGroupOne = new FundingGroup { OrganisationGroupIdentifierValue = Identifier1 };
            _fundingGroupTwo = new FundingGroup { OrganisationGroupIdentifierValue = Identifier2 };
            _results = new List<OrganisationGroupResult>
            {
                new OrganisationGroupResult { Name = "Name1", GroupReason = OrganisationGroupingReason.Payment, GroupTypeClassification = OrganisationGroupTypeClassification.GeographicalBoundary, GroupTypeCode = OrganisationGroupTypeCode.AcademyTrust, GroupTypeIdentifier = OrganisationGroupTypeIdentifier.AcademyTrustCode, IdentifierValue = Identifier1, SearchableName = "Name1" },
                new OrganisationGroupResult { Name = "Name2", GroupReason = OrganisationGroupingReason.Information, GroupTypeClassification = OrganisationGroupTypeClassification.GeographicalBoundary, GroupTypeCode = OrganisationGroupTypeCode.AcademyTrust, GroupTypeIdentifier = OrganisationGroupTypeIdentifier.AcademyTrustCode, IdentifierValue = Identifier2, SearchableName = "Name2" }
            };
            _service = new FundingGroupService(_releaseManagementRepository.Object, _context.Object, _logger.Object);
        }

        [TestMethod]
        public async Task WhenNoExistingFundingGroupsTheyAreCreated()
        {
            GivenGroupingReasons();
            GivenNoExistingFundingGroups();

            IEnumerable<FundingGroup> result = await _service.CreateFundingGroups(_specificationId, _channelId, _results);

            result
                .Should()
                .HaveCount(_results.Count);

            _releaseManagementRepository.Verify(r => r.CreateFundingGroupUsingAmbientTransaction(It.IsAny<FundingGroup>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task WhenSomeExistingFundingGroupsSomeAreCreated()
        {
            GivenGroupingReasons();
            GivenExistingFundingGroup(_fundingGroupOne, _fundingGroupOne.OrganisationGroupIdentifierValue);

            IEnumerable<FundingGroup> result = await _service.CreateFundingGroups(_specificationId, _channelId, _results);

            result
                .Should()
                .HaveCount(_results.Count);

            _releaseManagementRepository.Verify(r => r.CreateFundingGroupUsingAmbientTransaction(It.IsAny<FundingGroup>()), Times.Exactly(1));
        }

        [TestMethod]
        public void WhenGroupingReasonNotFound_ThrowsAndLogsInvalidOperationException()
        {
            GivenNoGroupingReasons();
            GivenNoExistingFundingGroups();

            Func<Task> result = async () => await _service.CreateFundingGroups(_specificationId, _channelId, _results);

            result
                .Should()
                .ThrowExactly<InvalidOperationException>();

            _releaseManagementRepository.Verify(r => r.CreateFundingGroup(It.IsAny<FundingGroup>()), Times.Never);
            _logger.Verify(l => l.Error(It.IsAny<string>()), Times.Once);
        }

        private void GivenGroupingReasons()
        {
            _releaseManagementRepository.Setup(r => r.GetGroupingReasons())
                .ReturnsAsync(new List<Publishing.FundingManagement.SqlModels.GroupingReason>
                {
                    new Publishing.FundingManagement.SqlModels.GroupingReason { GroupingReasonId = 1, GroupingReasonCode = "Payment", GroupingReasonName = "Payment" },
                    new Publishing.FundingManagement.SqlModels.GroupingReason { GroupingReasonId = 2, GroupingReasonCode = "Information", GroupingReasonName = "Information" }
                });
        }

        private void GivenNoGroupingReasons()
        {
            _releaseManagementRepository.Setup(r => r.GetGroupingReasons())
                .ReturnsAsync(new List<Publishing.FundingManagement.SqlModels.GroupingReason>());
        }

        private void GivenNoExistingFundingGroups()
        {
            _releaseManagementRepository.Setup(r => r.GetFundingGroup(
                It.Is<int>(s => s == _channelId),
                It.Is<string>(s => s == _specificationId),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync((FundingGroup)null);
        }

        private void GivenExistingFundingGroup(FundingGroup fundingGroup, string identifier)
        {
            _releaseManagementRepository.Setup(r => r.GetFundingGroup(
                It.Is<int>(s => s == _channelId),
                It.Is<string>(s => s == _specificationId),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.Is<string>(s => s == identifier)))
                .ReturnsAsync(fundingGroup);
        }
    }
}
