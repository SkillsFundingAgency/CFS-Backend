using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Provider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class FundingGroupDataGeneratorTests
    {
        private Mock<IPublishedFundingGenerator> _publishedFundingGenerator;
        private Mock<IPublishedProviderLoaderForFundingGroupData> _publishedProviderLoader;
        private Mock<IPoliciesService> _policiesService;
        private Mock<IPublishedFundingDateService> _publishedFundingDateService;
        private Mock<IReleaseManagementRepository> _releaseManagementRepository;
        private Mock<IPublishedFundingIdGeneratorResolver> _publishedFundingIdGeneratorResolver;
        private Mock<IPublishedFundingIdGenerator> _publishedFundingIdGenerator;
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
        private List<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)> _publishedFunding;
        private FundingGroupDataGenerator _service;
        private string _fundingPeriodId;
        private Reference _author;
        private string _jobId;
        private string _correlationId;
        private string _templateVersion;
        private TemplateMetadataContents _templateMetadataContents;
        private string _schemaVersion;
        private string _fundingId;

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
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();
            _publishedFundingIdGeneratorResolver = new Mock<IPublishedFundingIdGeneratorResolver>();
            _publishedFundingIdGenerator = new Mock<IPublishedFundingIdGenerator>();

            _templateVersion = new RandomString();
            _schemaVersion = new RandomString();

            _templateMetadataContents = new TemplateMetadataContents()
            {
                TemplateVersion = _templateVersion,
                FundingPeriodId = _specificationSummary.FundingPeriod.Id,
                FundingStreamId = _specificationSummary.FundingStreams.First().Id,
                SchemaVersion = _schemaVersion,
            };

            _service = new FundingGroupDataGenerator(
                _publishedFundingGenerator.Object,
                _policiesService.Object,
                _publishedFundingDateService.Object,
                _publishingResiliencePolicies,
                _releaseManagementRepository.Object,
                _publishedProviderLoader.Object,
                _publishedFundingIdGeneratorResolver.Object
            );

            _author = new Reference(new RandomString(), new RandomString());
            _jobId = new RandomString();
            _correlationId = new RandomString();
            _fundingId = new RandomString();
        }

        [TestMethod]
        public async Task GeneratesFundingGroupData_Successfully()
        {
            GivenTemplate();
            GivenPublishedProviders();
            GivenPublishedFunding();
            GivenSchemaVersionResolverExists();
            GivenFundingIdReturns();

            IEnumerable<GeneratedPublishedFunding> result = await _service.Generate(
                _organisationGroupsToCreate,
                _specificationSummary,
                _channel, new List<string> { new RandomString()
                },
                _author,
                _jobId,
                _correlationId);

            result
                .Should()
                .HaveCount(_organisationGroupsToCreate.Count);

            result
                .First()
                .PublishedFundingVersion.GroupingReason.ToString()
                .Should()
                .Equals(result.First().OrganisationGroupResult.GroupReason.ToString());

            result
                .First()
                .PublishedFundingVersion.OrganisationGroupTypeCode
                .Should()
                .Equals(result.First().OrganisationGroupResult.GroupTypeCode.ToString());

            result
                .First()
                .PublishedFundingVersion.OrganisationGroupTypeIdentifier.ToString()
                .Should()
                .Equals(result.First().OrganisationGroupResult.GroupTypeIdentifier.ToString());

            result
                .First()
                .PublishedFundingVersion.VariationReasons
                .Should()
                .BeEquivalentTo(new List<CalculateFunding.Models.Publishing.VariationReason>
                {
                    CalculateFunding.Models.Publishing.VariationReason.CalculationValuesUpdated,
                    CalculateFunding.Models.Publishing.VariationReason.CountryNameFieldUpdated
                });

            result
                .First()
                .PublishedFundingVersion.FundingId
                .Should()
                .Be(_fundingId);
        }

        private void GivenFundingIdReturns()
        {
            _publishedFundingIdGenerator.Setup(_ => _.GetFundingId(_publishedFunding[0].PublishedFundingVersion)).Returns(
                _fundingId);
        }

        private void GivenSchemaVersionResolverExists()
        {
            _publishedFundingIdGeneratorResolver
                .Setup(_ => _.GetService(_schemaVersion))
                .Returns(_publishedFundingIdGenerator.Object);
        }

        [TestMethod]
        public void WhenTemplateNotFound_Throws()
        {
            GivenNoTemplate();
            GivenPublishedProviders();
            GivenPublishedFunding();

            Func<Task> result = () => _service.Generate(
                _organisationGroupsToCreate,
                _specificationSummary,
                _channel,
                new List<string> { new RandomString() },
                _author,
                _jobId,
                _correlationId);

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
                _organisationGroupsToCreate, _specificationSummary,
                _channel,
                new List<string> { new RandomString() },
                _author,
                _jobId,
                _correlationId);

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
                            FundingStreamId = _fundingStream.Id,
                            FundingPeriodId = _specificationSummary.FundingPeriod.Id,
                            TemplateVersion = _templateVersion,
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
                OrganisationGroupIdentifierValue = groupIdentifier,
                Version = new RandomNumberBetween(1, 10),
                FundingId = new RandomString(),
                SchemaVersion = _schemaVersion,
            };

            _releaseManagementRepository.Setup(_ => _.GetLatestFundingGroupMajorVersionsBySpecificationId(_specificationSummary.Id, _channel.ChannelId))
                .ReturnsAsync(Enumerable.Empty<LatestFundingGroupVersion>());

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

            _publishedFunding =
               new List<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)>
               {
                    (new PublishedFunding { Current = publishedProviderVersion }, publishedProviderVersion)
               };

            _publishedFundingGenerator.Setup(_ => _.GeneratePublishedFunding(
                It.IsAny<PublishedFundingInput>(),
                It.IsAny<IEnumerable<PublishedProvider>>(),
                _author,
                _jobId,
                _correlationId))
                .Returns(_publishedFunding);
        }

        private void GivenNoTemplate()
        {
            _policiesService.Setup(_ => _.GetTemplateMetadataContents(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((TemplateMetadataContents)null);
        }

        private void GivenTemplate()
        {
            _policiesService.Setup(_ => _.GetTemplateMetadataContents(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(_templateMetadataContents);
        }
    }
}
