using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FundingGroup = CalculateFunding.Services.Publishing.FundingManagement.SqlModels.FundingGroup;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class FundingGroupServiceTests
    {
        private const string Identifier1 = "1";
        private const string Identifier2 = "2";
        private const string Identifier3 = "3";
        private const OrganisationGroupTypeClassification Classification1 = OrganisationGroupTypeClassification.GeographicalBoundary;
        private const OrganisationGroupTypeClassification LegalEntityClassification = OrganisationGroupTypeClassification.LegalEntity;

        private RandomString _specificationId;
        private RandomNumberBetween _channelId;

        private Mock<IReleaseManagementRepository> _releaseManagementRepository;
        private Mock<IReleaseToChannelSqlMappingContext> _context;
        private Mock<ILogger> _logger;
        private List<OrganisationGroupResult> _orgResults;
        private Mock<IUniqueIdentifierProvider> _identifierGenerator;
        private FundingGroupService _service;

        [TestInitialize]
        public void Initialise()
        {
            _specificationId = new RandomString();
            _channelId = new RandomNumberBetween(1, 10);
            _logger = new Mock<ILogger>();
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();
            _context = new Mock<IReleaseToChannelSqlMappingContext>();
            _context.SetupGet(s => s.FundingGroups)
                .Returns(new Dictionary<OrganisationGroupResult, Guid>());

            _orgResults = new List<OrganisationGroupResult>
            {
                new()
                {
                    Name = "Name1",
                    GroupReason = OrganisationGroupingReason.Payment,
                    GroupTypeClassification = Classification1,
                    GroupTypeCode = OrganisationGroupTypeCode.AcademyTrust,
                    GroupTypeIdentifier = OrganisationGroupTypeIdentifier.AcademyTrustCode,
                    IdentifierValue = Identifier1,
                    SearchableName = "Name1"
                },
                new()
                {
                    Name = "Name2",
                    GroupReason = OrganisationGroupingReason.Information,
                    GroupTypeClassification = LegalEntityClassification,
                    GroupTypeCode = OrganisationGroupTypeCode.AcademyTrust,
                    GroupTypeIdentifier = OrganisationGroupTypeIdentifier.AcademyTrustCode,
                    IdentifierValue = Identifier2,
                    SearchableName = "Name2"
                },
                new()
                {
                    Name = "Name3",
                    GroupReason = OrganisationGroupingReason.Information,
                    GroupTypeClassification = LegalEntityClassification,
                    GroupTypeCode = OrganisationGroupTypeCode.LocalAuthority,
                    GroupTypeIdentifier = OrganisationGroupTypeIdentifier.AcademyTrustCode,
                    IdentifierValue = Identifier3,
                    SearchableName = "Name3"
                }
            };

            _identifierGenerator = new Mock<IUniqueIdentifierProvider>();

            _service = new FundingGroupService(_releaseManagementRepository.Object,
                                               _context.Object,
                                               _identifierGenerator.Object,
                                               _logger.Object
                                               );

        }

        [TestMethod]
        public async Task WhenNoExistingFundingGroupsTheyAreCreated()
        {
            GivenGroupingReasons();
            GivenExistingFundingGroups(new List<FundingGroup>());

            await _service.CreateFundingGroups(_specificationId, _channelId, _orgResults);

            _releaseManagementRepository.Verify(r => r.BulkCreateFundingGroupsUsingAmbientTransaction(
                Match.Create<IEnumerable<FundingGroup>>(_ => _.Count() == _orgResults.Count)), Times.Once);
        }

        [TestMethod]
        public async Task WhenExistingFundingGroupsNewOnesAreCreated()
        {
            GivenGroupingReasons();
            GivenExistingFundingGroups(new List<FundingGroup>
            {
                new()
                {
                    ChannelId = _channelId,
                    SpecificationId = _specificationId,
                    GroupingReasonId = 1,
                    OrganisationGroupTypeClassification = Classification1.ToString(),
                    OrganisationGroupIdentifierValue = Identifier1,
                    OrganisationGroupTypeCode = "AcademyTrust",
                }
            });

            await _service.CreateFundingGroups(_specificationId, _channelId, _orgResults);

            const int expectedCreateCount = 2;

            _releaseManagementRepository.Verify(r => r.BulkCreateFundingGroupsUsingAmbientTransaction(
                    Match.Create<IEnumerable<FundingGroup>>(_ =>
                        _.Count() == expectedCreateCount && _.First().OrganisationGroupIdentifierValue == Identifier2)),
                Times.Once);
        }

        [TestMethod]
        public void WhenOrganisationGroupNotFound_ThrowsAndLogsInvalidOperationException()
        {
            GivenGroupingReasons();
            GivenWrongFundingGroups();

            Func<Task> result = async () => await _service.CreateFundingGroups(_specificationId, _channelId, _orgResults);

            result
                .Should()
                .ThrowExactly<KeyNotFoundException>();

            _logger.Verify(l => l.Error(It.IsAny<string>()), Times.Once);
        }

        private void GivenGroupingReasons()
        {
            _releaseManagementRepository.Setup(r => r.GetGroupingReasons())
                .ReturnsAsync(new List<Publishing.FundingManagement.SqlModels.GroupingReason>
                {
                    new() { GroupingReasonId = 1, GroupingReasonCode = "Payment", GroupingReasonName = "Payment" },
                    new() { GroupingReasonId = 2, GroupingReasonCode = "Information", GroupingReasonName = "Information" }
                });
        }

        private void GivenExistingFundingGroups(IEnumerable<FundingGroup> fundingGroups)
        {
            _releaseManagementRepository.Setup(r => r.GetFundingGroupsBySpecificationAndChannelUsingAmbientTransaction(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(fundingGroups);
        }

        private void GivenWrongFundingGroups()
        {
            _releaseManagementRepository.Setup(r => r.BulkCreateFundingGroupsUsingAmbientTransaction(It.IsAny<IEnumerable<FundingGroup>>()))
                .ReturnsAsync(new List<FundingGroup> { new()
                {
                    FundingGroupId = Guid.NewGuid(),
                    OrganisationGroupTypeClassification = new RandomString(),
                    OrganisationGroupIdentifierValue = new RandomString()
                }});
        }
    }
}
