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
using FluentAssertions;
using Microsoft.FeatureManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class ChannelOrganisationGroupGeneratorServiceTests
    {
        private Mock<IOrganisationGroupGenerator> _generator;
        private Mock<IFeatureManager> _featureManager;
        private ChannelOrganisationGroupGeneratorService _sut;

        [TestInitialize]
        public void Initialise()
        {
            _generator = new Mock<IOrganisationGroupGenerator>();
            _featureManager = new Mock<IFeatureManager>();
            IMapper mapper = CreateMapper();
            _featureManager
                .Setup(_ => _.IsEnabledAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            _sut = new ChannelOrganisationGroupGeneratorService(_generator.Object, mapper, _featureManager.Object);
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

        [TestMethod]
        public void WhenGeneratingOrganisationGroupsWithMissingGroupingConfiguration_ThenExceptionThrown()
        {
            string channelCode = "MissingChannel";
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
                        ChannelCode =   "ConfiguredChannel",
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

            Func<Task> action = async () =>
            {
                IEnumerable<OrganisationGroupResult> result = await _sut.GenerateOrganisationGroups(
                    channel, fundingConfiguration, specification, batchProviders);
            };

            action
                .Should()
                .Throw<InvalidOperationException>()
                .WithMessage("The organisation group configuration is missing for channel with code 'MissingChannel'");
        }

        [TestMethod]
        public async Task WhenGeneratingOrganisationGroupsForAllChannels_GeneratorRunsForEachChannel()
        {
            List<OrganisationGroupResult> expectedOrganisationGroupResults = new List<OrganisationGroupResult>();

            _generator.Setup(g => g.GenerateOrganisationGroup(
                It.IsAny<List<OrganisationGroupingConfiguration>>(),
                It.IsAny<ProviderSource>(),
                It.IsAny<PaymentOrganisationSource>(),
                It.IsAny<IEnumerable<ApiProvider>>(),
                It.IsAny<string>(),
                It.IsAny<int?>()))
                .ReturnsAsync(expectedOrganisationGroupResults);

            FundingConfiguration fundingConfiguration = NewFundingConfiguration();
            SpecificationSummary specificationSummary = NewSpecificationSummary();

            IDictionary<string, IEnumerable<OrganisationGroupResult>> result = await _sut.GenerateOrganisationGroupsForAllChannels(
                fundingConfiguration, specificationSummary, NewBatchProviders());

            _generator.Verify(g => g.GenerateOrganisationGroup(
                It.IsAny<IEnumerable<OrganisationGroupingConfiguration>>(),
                It.Is<ProviderSource>(ps => ps.Equals(fundingConfiguration.ProviderSource)),
                It.Is<PaymentOrganisationSource>(pos => pos.Equals(fundingConfiguration.PaymentOrganisationSource)),
                It.IsAny<IEnumerable<ApiProvider>>(),
                It.Is<string>(s => s.Equals(specificationSummary.ProviderVersionId)),
                It.Is<int?>(s => s.Equals(specificationSummary.ProviderSnapshotId))),
                Times.Exactly(fundingConfiguration.ReleaseChannels.Count()));

            Assert.AreEqual(result.Count, fundingConfiguration.ReleaseChannels.Count());
        }

        private FundingConfiguration NewFundingConfiguration()
        {
            List<OrganisationGroupingConfiguration> organisationGroupingConfigurations = new List<OrganisationGroupingConfiguration>
            {
                new OrganisationGroupingConfiguration
                {
                    GroupingReason = Common.ApiClient.Policies.Models.GroupingReason.Contracting,
                    GroupTypeClassification = OrganisationGroupTypeClassification.LegalEntity,
                    GroupTypeIdentifier = OrganisationGroupTypeIdentifier.UKPRN,
                    OrganisationGroupTypeCode = OrganisationGroupTypeCode.LocalAuthoritySsf
                },
                new OrganisationGroupingConfiguration
                {
                    GroupingReason = Common.ApiClient.Policies.Models.GroupingReason.Contracting,
                    GroupTypeClassification = OrganisationGroupTypeClassification.LegalEntity,
                    GroupTypeIdentifier = OrganisationGroupTypeIdentifier.UKPRN,
                    OrganisationGroupTypeCode = OrganisationGroupTypeCode.LocalAuthority
                },
                new OrganisationGroupingConfiguration
                {
                    GroupingReason = Common.ApiClient.Policies.Models.GroupingReason.Information,
                    GroupTypeClassification = OrganisationGroupTypeClassification.GeographicalBoundary,
                    GroupTypeIdentifier = OrganisationGroupTypeIdentifier.LACode,
                    OrganisationGroupTypeCode = OrganisationGroupTypeCode.LocalAuthority
                },
                new OrganisationGroupingConfiguration
                {
                    GroupingReason = Common.ApiClient.Policies.Models.GroupingReason.Payment,
                    GroupTypeClassification = OrganisationGroupTypeClassification.LegalEntity,
                    GroupTypeIdentifier = OrganisationGroupTypeIdentifier.UKPRN,
                    OrganisationGroupTypeCode = OrganisationGroupTypeCode.AcademyTrust
                },
                new OrganisationGroupingConfiguration
                {
                    GroupingReason = Common.ApiClient.Policies.Models.GroupingReason.Contracting,
                    GroupTypeClassification = OrganisationGroupTypeClassification.LegalEntity,
                    GroupTypeIdentifier = OrganisationGroupTypeIdentifier.UKPRN,
                    OrganisationGroupTypeCode = OrganisationGroupTypeCode.Provider
                },
                new OrganisationGroupingConfiguration
                {
                    GroupingReason = Common.ApiClient.Policies.Models.GroupingReason.Indicative,
                    GroupTypeClassification = OrganisationGroupTypeClassification.LegalEntity,
                    GroupTypeIdentifier = OrganisationGroupTypeIdentifier.UKPRN,
                    OrganisationGroupTypeCode = OrganisationGroupTypeCode.Provider
                }
            };

            return new FundingConfiguration
            {
                ProviderSource = ProviderSource.CFS,
                PaymentOrganisationSource = PaymentOrganisationSource.PaymentOrganisationAsProvider,
                ReleaseChannels = new List<FundingConfigurationChannel>
                {
                    new FundingConfigurationChannel
                    {
                        ChannelCode = "ChannelOne",
                        OrganisationGroupings = organisationGroupingConfigurations
                    },
                    new FundingConfigurationChannel
                    {
                        ChannelCode = "ChannelTwo",
                        OrganisationGroupings = organisationGroupingConfigurations
                    }
                }
            };
        }

        private SpecificationSummary NewSpecificationSummary(string providerVersionId = null, int? providerSnapshotId = null)
        {
            return new SpecificationSummary
            {
                ProviderVersionId = providerVersionId ?? "123",
                ProviderSnapshotId = providerSnapshotId ?? 100
            };
        }

        private List<PublishedProviderVersion> NewBatchProviders()
        {
            return new List<PublishedProviderVersion>
            {
                new PublishedProviderVersion() { FundingStreamId = "Stream1", FundingPeriodId = "Period1" },
                new PublishedProviderVersion() { FundingStreamId = "Stream1", FundingPeriodId = "Period2" }
            };
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
