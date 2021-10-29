using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalculateFunding.Generators.OrganisationGroup.Models;
using Moq;
using Polly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Services.Publishing.Models;
using Provider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;
using System.Linq;
using FluentAssertions;
using CalculateFunding.Services.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class FundingGroupDataGeneratorTests
    {
        private Mock<IPublishedFundingGenerator> _publishedFundingGenerator;
        private Mock<IPublishedProviderLoaderForFundingGroupData> _publishedProviderLoader;
        private Mock<IPoliciesService> _policiesService;
        private Mock<IPublishedFundingDateService> _publishedFundingDateService;
        private Mock<IPublishedFundingDataService> _publishedFundingDataService;
        private readonly IPublishingResiliencePolicies _publishingResiliencePolicies = new ResiliencePolicies
        {
            PublishedFundingRepository = Policy.NoOpAsync()
        };
        private readonly Reference _fundingStream = new Reference(new RandomString(), new RandomString());
        private readonly Channel _channel = new Channel
        {
            ChannelId = new RandomNumberBetween(1, 10),
            ChannelCode = new RandomString(),
            ChannelName = new RandomString()
        };
        private string _providerId = new RandomString();
        private SpecificationSummary _specificationSummary;
        private List<OrganisationGroupResult> _organisationGroupsToCreate;
        private FundingGroupDataGenerator _service;

        [TestInitialize]
        public void Initialise()
        {
            _specificationSummary = new SpecificationSummary
            {
                FundingStreams = new List<Reference>
                {
                    _fundingStream
                },
                FundingPeriod = new Reference { Id = new RandomString(), Name = new RandomString() },
                TemplateIds = new Dictionary<string, string>
                {
                    { _fundingStream.Id, new RandomString() }
                }
            };

            _publishedFundingGenerator = new Mock<IPublishedFundingGenerator>();
            _publishedProviderLoader = new Mock<IPublishedProviderLoaderForFundingGroupData>();
            _policiesService = new Mock<IPoliciesService>();
            _publishedFundingDateService = new Mock<IPublishedFundingDateService>();
            _publishedFundingDataService = new Mock<IPublishedFundingDataService>();
            _service = new FundingGroupDataGenerator(
                _publishedFundingGenerator.Object,
                _policiesService.Object,
                _publishedFundingDateService.Object,
                _publishingResiliencePolicies,
                _publishedFundingDataService.Object,
                _publishedProviderLoader.Object
            );
        }

        [TestMethod]
        public async Task GeneratesFundingGroupData_Successfully()
        {
            GivenTemplate();
            GivenPublishedProviders();
            GivenPublishedFunding();

            IEnumerable<(PublishedFundingVersion, OrganisationGroupResult)> result = await _service.Generate(
                _organisationGroupsToCreate, _specificationSummary, _channel, new List<string> { new RandomString() });

            result
                .Should()
                .HaveCount(_organisationGroupsToCreate.Count);

            result
                .First()
                .Item1.GroupingReason.ToString()
                .Should()
                .Equals(result.First().Item2.GroupReason.ToString());

            result
                .First()
                .Item1.OrganisationGroupTypeCode
                .Should()
                .Equals(result.First().Item2.GroupTypeCode.ToString());

            result
                .First()
                .Item1.OrganisationGroupTypeIdentifier.ToString()
                .Should()
                .Equals(result.First().Item2.GroupTypeIdentifier.ToString());

            result
                .First()
                .Item1.VariationReasons
                .Should()
                .BeEquivalentTo(new List<CalculateFunding.Models.Publishing.VariationReason>
                {
                    CalculateFunding.Models.Publishing.VariationReason.CalculationValuesUpdated,
                    CalculateFunding.Models.Publishing.VariationReason.CountryNameFieldUpdated
                });
        }

        [TestMethod]
        public void WhenTemplateNotFound_Throws()
        {
            GivenNoTemplate();
            GivenPublishedProviders();
            GivenPublishedFunding();

            Func<Task> result = () => _service.Generate(
                _organisationGroupsToCreate, _specificationSummary, _channel, new List<string> { new RandomString() });

            result
                .Should()
                .ThrowExactly<NonRetriableException>()
                .WithMessage($"Unable to get template metadata contents for funding stream. '{_fundingStream.Id}'");
        }

        [TestMethod]
        public void WhenPublishedFundingMissing_Throws()
        {
            GivenTemplate();
            GivenPublishedProviders();
            GivenPublishedFunding(setMissingPublishedFunding: true);

            Func<Task> result = () => _service.Generate(
                _organisationGroupsToCreate, _specificationSummary, _channel, new List<string> { new RandomString() });

            result
                .Should()
                .ThrowExactly<NonRetriableException>();
        }

        private void GivenPublishedProviders()
        {
            _publishedProviderLoader.Setup(_ => _.GetAllPublishedProviders(
                It.IsAny<IEnumerable<OrganisationGroupResult>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new List<PublishedProvider>
                {
                    new PublishedProvider
                    {
                        Current = new PublishedProviderVersion
                        {
                            ProviderId = _providerId,
                            VariationReasons = new List<CalculateFunding.Models.Publishing.VariationReason>
                            {
                                CalculateFunding.Models.Publishing.VariationReason.CalculationValuesUpdated,
                                CalculateFunding.Models.Publishing.VariationReason.CountryNameFieldUpdated
                            }
                        },
                    }
                });
        }

        private void GivenPublishedFunding(bool setMissingPublishedFunding = false)
        {
            string groupIdentifier = new RandomString();
            OrganisationGroupingReason groupingReason = OrganisationGroupingReason.Contracting;
            OrganisationGroupTypeClassification groupTypeClassification = OrganisationGroupTypeClassification.GeographicalBoundary;
            OrganisationGroupTypeCode groupTypeCode = OrganisationGroupTypeCode.AcademyTrust;
            OrganisationGroupTypeIdentifier groupTypeIdentifier = OrganisationGroupTypeIdentifier.AcademyTrustCode;

            PublishedFundingVersion publishedProviderVersion = new PublishedFundingVersion
            {
                FundingStreamId = _fundingStream.Id,
                FundingPeriod = new PublishedFundingPeriod { Id = new RandomString() },
                GroupingReason = Enum.Parse<CalculateFunding.Models.Publishing.GroupingReason>(groupingReason.ToString()),
                OrganisationGroupTypeCode = groupTypeCode.ToString(),
                OrganisationGroupTypeIdentifier = groupTypeIdentifier.ToString(),
                Version = new RandomNumberBetween(1, 10)
            };

            _publishedFundingDataService.Setup(_ => _.GetCurrentPublishedFunding(It.IsAny<string>(), It.IsAny<GroupingReason?>()))
                .ReturnsAsync(new List<PublishedFunding>
                {
                    new PublishedFunding
                    {
                        Current = publishedProviderVersion
                    }
                });

            _organisationGroupsToCreate = new List<OrganisationGroupResult>
            {
                new OrganisationGroupResult {
                    Providers = new List<Provider> { new Provider { ProviderId = _providerId } },
                    IdentifierValue = groupIdentifier,
                    GroupReason = setMissingPublishedFunding ? OrganisationGroupingReason.Information : groupingReason,
                    GroupTypeClassification = groupTypeClassification,
                    GroupTypeCode = groupTypeCode,
                    GroupTypeIdentifier = groupTypeIdentifier
                }
            };

            List<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)> publishedFunding =
                new List<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)>
                {
                    (new PublishedFunding { Current = publishedProviderVersion }, publishedProviderVersion)
                };

            _publishedFundingGenerator.Setup(_ => _.GeneratePublishedFunding(It.IsAny<PublishedFundingInput>(), It.IsAny<IEnumerable<PublishedProvider>>()))
                .Returns(publishedFunding);
        }

        private void GivenNoTemplate()
        {
            _policiesService.Setup(_ => _.GetTemplateMetadataContents(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((TemplateMetadataContents)null);
        }

        private void GivenTemplate()
        {
            _policiesService.Setup(_ => _.GetTemplateMetadataContents(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new TemplateMetadataContents());
        }
    }
}
