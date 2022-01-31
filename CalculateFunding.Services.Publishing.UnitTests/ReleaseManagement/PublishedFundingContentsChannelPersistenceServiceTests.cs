using CalculateFunding.Common.Storage;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class PublishedFundingContentsChannelPersistenceServiceTests
    {
        private IPublishedFundingContentsChannelPersistenceService _publishedFundingContentsChannelPersistenceService;
        private ILogger _logger;
        private IPublishedProviderChannelVersionService _publishedProviderChannelVersionService;
        private IPoliciesService _policiesService;
        private IPublishedFundingContentsGeneratorResolver _publishedFundingContentsGeneratorResolver;

        private PublishedFundingPeriod _publishedFundingPeriod;
        private const string ProviderVersionFundingStreamId = "providerVersionFundingStreamId";
        
        private IBlobClient _blobClient;

        [TestInitialize]
        public void SetUp()
        {
            _blobClient = Substitute.For<IBlobClient>();
            _logger = Substitute.For<ILogger>();
            _publishedProviderChannelVersionService = Substitute.For<IPublishedProviderChannelVersionService>();
            IConfiguration configuration = Substitute.For<IConfiguration>();
            _policiesService = Substitute.For<IPoliciesService>();

            _publishedFundingContentsGeneratorResolver = Substitute.For<IPublishedFundingContentsGeneratorResolver>();

            _publishedFundingContentsChannelPersistenceService = new PublishedFundingContentsChannelPersistenceService(
                _logger,
                _publishedFundingContentsGeneratorResolver,
                _blobClient,
                new ResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync(),
                    PublishedFundingBlobRepository = Policy.NoOpAsync()
                },
                new PublishingEngineOptions(configuration),
                _policiesService);

            _publishedFundingPeriod = new PublishedFundingPeriod { Id = $"{PublishedFundingPeriodType.AY}-123", Type = PublishedFundingPeriodType.AY, Period = "123" };
        }

        [TestMethod]
        public async Task SavesPublishedFundingWhenCorrectInputGiven()
        {
            // Arrange
            IPublishedFundingContentsGenerator publishedFundingContentsGenerator = Substitute.For<IPublishedFundingContentsGenerator>();

            PublishedFundingVersion publishedFundingVersion = NewPublishedFundingVersion(fundingVersion => fundingVersion
                .WithFundingId("funding1")
                .WithFundingPeriod(_publishedFundingPeriod)
                .WithFundingStreamId(ProviderVersionFundingStreamId)
                .WithGroupReason(CalculateFunding.Models.Publishing.GroupingReason.Payment)
                .WithOrganisationGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
                .WithOrganisationGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode)
                .WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
                .WithOrganisationGroupIdentifierValue("101"));

            List<PublishedFundingVersion> publishedFundingVersions = new List<PublishedFundingVersion> { publishedFundingVersion };

            string channelCode = NewRandomString();
            Channel channel = new Channel { ChannelCode = channelCode };

            string schemaVersionKey
                    = $"{publishedFundingVersion.FundingStreamId}-{publishedFundingVersion.FundingPeriod.Id}-{publishedFundingVersion.TemplateVersion}".ToLower();

            string schemaVersion = NewRandomString();
            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _.WithSchemeVersion(schemaVersion));

            _policiesService
                .GetTemplateMetadataContents(publishedFundingVersion.FundingStreamId, publishedFundingVersion.FundingPeriod.Id, publishedFundingVersion.TemplateVersion)
                .Returns(templateMetadataContents);

            _publishedFundingContentsGeneratorResolver
                .GetService(schemaVersion)
                .Returns(publishedFundingContentsGenerator);

            publishedFundingContentsGenerator
                .GenerateContents(publishedFundingVersion, templateMetadataContents)
                .Returns("contents");


            // Act
            await _publishedFundingContentsChannelPersistenceService.SavePublishedFundingContents(
                publishedFundingVersions, channel);


            // Assert
            _logger
                .Received()
                .Information("Published funding contents saved to blob");
        }

        private PublishedFundingVersion NewPublishedFundingVersion(Action<PublishedFundingVersionBuilder> setUp = null)
        {
            PublishedFundingVersionBuilder fundingVersionBuilder = new PublishedFundingVersionBuilder();

            setUp?.Invoke(fundingVersionBuilder);

            return fundingVersionBuilder.Build();
        }

        private TemplateMetadataContents NewTemplateMetadataContents(Action<TemplateMetadataContentsBuilder> setUp = null)
        {
            TemplateMetadataContentsBuilder templateMetadataContentsBuilder = new TemplateMetadataContentsBuilder();

            setUp?.Invoke(templateMetadataContentsBuilder);

            return templateMetadataContentsBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}
