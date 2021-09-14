using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class ChannelOrganisationGroupGeneratorServiceTests
    {
        private Mock<IOrganisationGroupGenerator> _generator;
        private ChannelOrganisationGroupGeneratorService _sut;

        [TestInitialize]
        public void Initialise()
        {
            _generator = new Mock<IOrganisationGroupGenerator>();
            IMapper mapper = CreateMapper();

            _sut = new ChannelOrganisationGroupGeneratorService(_generator.Object, mapper);
        }

        [TestMethod]
        public async Task WhenGeneratingOrganisationGroups_DelegatesToGeneratorWithCorrectValues()
        {
            string channelCode = "ABC";
            string providerVersionId = "123";
            int providerSnapshotId = 100;
            ProviderSource providerSource = ProviderSource.CFS;
            PaymentOrganisationSource paymentOrganisationSource = PaymentOrganisationSource.PaymentOrganisationAsProvider;
            Channel channel = new Channel
            {
                ChannelCode = channelCode
            };
            List<OrganisationGroupingConfiguration> organisationGroupingConfigurations = new List<OrganisationGroupingConfiguration>
            {
                new OrganisationGroupingConfiguration
                {
                    GroupingReason = Common.ApiClient.Policies.Models.GroupingReason.Contracting,
                    GroupTypeClassification = OrganisationGroupTypeClassification.GeographicalBoundary,
                    GroupTypeIdentifier = OrganisationGroupTypeIdentifier.AcademyTrustCode,
                    OrganisationGroupTypeCode = OrganisationGroupTypeCode.AcademyTrust
                }
            };
            FundingConfiguration fundingConfiguration = new FundingConfiguration
            {
                ProviderSource = providerSource,
                PaymentOrganisationSource = paymentOrganisationSource,
                ReleaseChannels = new List<FundingConfigurationChannel>
                {
                    new FundingConfigurationChannel
                    {
                        ChannelCode = channelCode,
                        OrganisationGroupings = organisationGroupingConfigurations
                    }
                }
            };
            SpecificationSummary specification = new SpecificationSummary
            {
                ProviderVersionId = providerVersionId,
                ProviderSnapshotId = providerSnapshotId
            };
            List<PublishedProviderVersion> batchProviders = new List<PublishedProviderVersion>
            {
                new PublishedProviderVersion(),
                new PublishedProviderVersion()
            };
            List<OrganisationGroupResult> expectedOrganisationGroupResults = new List<OrganisationGroupResult>();

            _generator.Setup(g => g.GenerateOrganisationGroup(
                It.IsAny<List<OrganisationGroupingConfiguration>>(),
                It.IsAny<ProviderSource>(),
                It.IsAny<PaymentOrganisationSource>(),
                It.IsAny<IEnumerable<ApiProvider>>(),
                It.IsAny<string>(),
                It.IsAny<int?>()))
                .ReturnsAsync(expectedOrganisationGroupResults);

            IEnumerable<OrganisationGroupResult> result = await _sut.GenerateOrganisationGroups(
                channel, fundingConfiguration, specification, batchProviders);

            _generator.Verify(g => g.GenerateOrganisationGroup(
                It.Is<IEnumerable<OrganisationGroupingConfiguration>>(
                    o => o.Equals(organisationGroupingConfigurations)),
                It.Is<ProviderSource>(ps => ps.Equals(providerSource)),
                It.Is<PaymentOrganisationSource>(pos => pos.Equals(paymentOrganisationSource)),
                It.IsAny<IEnumerable<ApiProvider>>(),
                It.Is<string>(s => s.Equals(providerVersionId)),
                It.Is<int?>(s => s.Equals(providerSnapshotId))),
                Times.Once);
        }

        private IMapper CreateMapper()
        {
            MapperConfiguration config = new MapperConfiguration(c =>
            {
                c.AddProfile<PublishingServiceMappingProfile>();
            });

            return new Mapper(config);
        }
    }
}
