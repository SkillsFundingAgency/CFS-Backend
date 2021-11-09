using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class OrganisationGroupServiceTests : ServiceTestsBase
    {
        private OrganisationGroupService _organisationGroupService;

        private IMapper _mapper;
        private Mock<IOrganisationGroupGenerator> _organisationGroupGenerator;

        [TestInitialize]
        public void Setup()
        {
            MapperConfiguration mappingConfig = new MapperConfiguration(
                c => c.AddProfile<PublishingServiceMappingProfile>());
            _mapper = mappingConfig.CreateMapper();
            _organisationGroupGenerator = new Mock<IOrganisationGroupGenerator>();

            _organisationGroupService = new OrganisationGroupService(
                _mapper,
                _organisationGroupGenerator.Object
                );
        }

        [TestMethod]
        public async Task GeneratesOrganisationGroups()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string providerVersionId = NewRandomString();
            int? providerSnapshotId = NewRandomNumber();

            FundingConfiguration fundingConfiguration = NewFundingConfiguration();

            IEnumerable<Provider> providers = Array.Empty<Provider>();
            PublishedProvider publishedProvider = NewPublishedProvider(_ => _
                .WithCurrent(
                    NewPublishedProviderVersion(ppv => ppv
                        .WithFundingStreamId(fundingStreamId)
                        .WithFundingPeriodId(fundingPeriodId))));

            IEnumerable<PublishedProvider> publishedProviders = new[] { publishedProvider };

            OrganisationGroupResult organisationGroupResult = NewOrganisationGroupResult();
            IEnumerable<OrganisationGroupResult> organisationGroupResults = new[] { organisationGroupResult };

            GivenGenerateOrganisationGroup(
                fundingConfiguration, 
                providerVersionId, 
                providerSnapshotId, 
                organisationGroupResults);

            Dictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData
                = await _organisationGroupService.GenerateOrganisationGroups(
                    providers,
                    publishedProviders,
                    fundingConfiguration,
                    providerVersionId,
                    providerSnapshotId);

            string organisationGroupsKey = $"{fundingStreamId}:{fundingPeriodId}";


            organisationGroupResultsData.Should().ContainKey(organisationGroupsKey);
            IEnumerable<OrganisationGroupResult> actualOrganisationGroupResults
                = organisationGroupResultsData[organisationGroupsKey];

            actualOrganisationGroupResults.Should().HaveCount(1);
            actualOrganisationGroupResults.FirstOrDefault().Should().Be(organisationGroupResult);
        }

        public void GivenGenerateOrganisationGroup(
            FundingConfiguration fundingConfiguration,
            string providerVersionId,
            int? providerSnapshotId,
            IEnumerable<OrganisationGroupResult> organisationGroupResults)
        {
            _organisationGroupGenerator
                .Setup(_ => _.GenerateOrganisationGroup(
                    fundingConfiguration,
                    It.IsAny<IEnumerable<Common.ApiClient.Providers.Models.Provider>>(),
                    providerVersionId,
                    providerSnapshotId))
                .ReturnsAsync(organisationGroupResults);
        }
    }
}
