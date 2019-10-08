using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedFundingContentsPersistanceServiceTests
    {
        private PublishedFundingContentsPersistanceService _publishedFundingContentsPersistanceService;
        private IBlobClient _blobClient;
        private const string _fundingStream = "stream1";
        private const string _schema = "1.0";
        private PublishedFundingPeriod _publishedFundingPeriod;
        private IPublishedFundingContentsGenerator _publishedFundingContentsGenerator;
        private Common.TemplateMetadata.Models.TemplateMetadataContents _templateMetadataContents;
        private PublishedFundingVersion _publishedFundingVersion;

        [TestInitialize]
        public void SetUp()
        {
            IPublishedFundingContentsGeneratorResolver publishedFundingContentsGeneratorResolver = Substitute.For<IPublishedFundingContentsGeneratorResolver>();
            _blobClient = Substitute.For<IBlobClient>();

            _templateMetadataContents = new Common.TemplateMetadata.Models.TemplateMetadataContents { SchemaVersion = _schema };

            _publishedFundingContentsGenerator = Substitute.For<IPublishedFundingContentsGenerator>();

            publishedFundingContentsGeneratorResolver.GetService(Arg.Is(_schema))
                .Returns(_publishedFundingContentsGenerator);

            _publishedFundingContentsPersistanceService = new PublishedFundingContentsPersistanceService(publishedFundingContentsGeneratorResolver, _blobClient, PublishingResilienceTestHelper.GenerateTestPolicies());

            _publishedFundingPeriod = new PublishedFundingPeriod { Type = PublishedFundingPeriodType.AY, Period = "123" };
        }

        [TestMethod]
        public async Task SavePublishedFundingContents_GivenPublishedFundingVersions_FileUploaded()
        {
            // Arrange
            _publishedFundingVersion = NewPublishedFundingVersion(version => version.WithFundingId("funding1")
            .WithFundingPeriod(_publishedFundingPeriod)
            .WithFundingStreamId(_fundingStream)
            .WithGroupReason(Models.Publishing.GroupingReason.Payment)
            .WithOrganisationGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithOrganisationGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode)
            .WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
            .WithOrganisationGroupIdentifierValue("101"));

            GivenTheGeneratedContentsIsReturned();

            // Act
            await WhenPublishedFundingContentsSaved();

            // Assert
            await ThenFileIsUploaded();
        }

        [TestMethod]
        public void SavePublishedFundingContents_GivenPublishedFundingVersionsButInvalidTemplate_ExceptionIsThrown()
        {
            // Arrange
            _publishedFundingVersion = NewPublishedFundingVersion(version => version.WithFundingId("funding1")
            .WithFundingPeriod(_publishedFundingPeriod)
            .WithFundingStreamId(_fundingStream)
            .WithGroupReason(Models.Publishing.GroupingReason.Payment)
            .WithOrganisationGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithOrganisationGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode)
            .WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
            .WithOrganisationGroupIdentifierValue("101"));

            // Act
            Func<Task> invocation = async () => await WhenPublishedFundingContentsSaved();

            // Assert
            ThenExceptionShouldBeThrown($"Generator failed to generate content for published provider version with id: '{_publishedFundingVersion.Id}'", invocation);
        }

        private void ThenExceptionShouldBeThrown(string message, Func<Task> invocation)
        {
            invocation
                .Should()
                .Throw<Exception>()
                .WithMessage(message);
        }

        private async Task ThenFileIsUploaded()
        {
            string blobName = $"{_fundingStream}-{_publishedFundingPeriod.Id}-{Models.Publishing.GroupingReason.Payment.ToString()}-{OrganisationGroupTypeCode.AcademyTrust}-{_publishedFundingVersion.OrganisationGroupIdentifierValue}-{1}_{0}.json";

            await _blobClient
                .Received(1)
                .UploadFileAsync(Arg.Is(blobName), Arg.Any<string>());
        }

        private void GivenTheGeneratedContentsIsReturned()
        {
            _publishedFundingContentsGenerator.GenerateContents(_publishedFundingVersion, _templateMetadataContents)
                .Returns("template1");
        }

        private async Task WhenPublishedFundingContentsSaved()
        {
            await _publishedFundingContentsPersistanceService.SavePublishedFundingContents(new List<PublishedFundingVersion> { _publishedFundingVersion }, _templateMetadataContents);
        }

        private PublishedFundingVersion NewPublishedFundingVersion(Action<PublishedFundingVersionBuilder> setUp = null)
        {
            PublishedFundingVersionBuilder providerFundingVersionBuilder = new PublishedFundingVersionBuilder();

            setUp?.Invoke(providerFundingVersionBuilder);

            return providerFundingVersionBuilder.Build();
        }
    }
}
