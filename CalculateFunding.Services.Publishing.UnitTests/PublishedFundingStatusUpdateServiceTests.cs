using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedFundingStatusUpdateServiceTests
    {
        private PublishedFundingStatusUpdateService _publishedFundingStatusUpdateService;
        private IPublishedFundingRepository _publishedFundingRepository;
        private IVersionRepository<PublishedFundingVersion> _publishedFundingVersionRepository;
        private PublishedFundingVersion _publishedFundingVersion;
        private PublishedFunding _publishedFunding;
        private PublishedFundingPeriod _publishedFundingPeriod;
        private Reference _author;
        private IPublishedFundingIdGeneratorResolver _publishedFundingIdGeneratorResolver;

        [TestInitialize]
        public void SetUp()
        {
            ILogger logger = Substitute.For<ILogger>();
            IConfiguration configuration = Substitute.For<IConfiguration>();
            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();
            _publishedFundingVersionRepository = Substitute.For<IVersionRepository<PublishedFundingVersion>>();
            _publishedFundingIdGeneratorResolver = Substitute.For<IPublishedFundingIdGeneratorResolver>();
            _author = new Reference { Id = "authorId", Name = "author" };

            _publishedFundingStatusUpdateService = new PublishedFundingStatusUpdateService(_publishedFundingRepository,
                                                                                           PublishingResilienceTestHelper.GenerateTestPolicies(),
                                                                                           _publishedFundingVersionRepository,
                                                                                           _publishedFundingIdGeneratorResolver,
                                                                                           logger,
                                                                                           new PublishingEngineOptions(configuration));

            _publishedFundingPeriod = new PublishedFundingPeriod { Type = PublishedFundingPeriodType.AY, Period = "123" };
        }

        [TestMethod]
        public async Task UpdatePublishedFundingStatus_GivenExistingPublishedFundingAndNewPublishedFundingVersionAndSaveSucceeds_NewVersionSaved()
        {
            GivenPublishedFunding();

            GivenNewVersionCreated();

            GivenPublishedFundingUpserted();

            await WhenStatusUpdated();

            await ThenNewVersionSaved();
        }

        [TestMethod]
        public void UpdatePublishedFundingStatus_GivenExistingPublishedFundingAndNewPublishedFundingVersionAndSaveFails_ExceptionThrown()
        {
            // Arrange
            GivenPublishedFunding();

            GivenNewVersionCreated();

            // Act
            Func<Task> invocation = async () => await WhenStatusUpdated();

            // Assert
            ThenExceptionShouldBeThrown($"Failed to save published funding for id: {_publishedFunding.Id} with status code 0", invocation);
        }

        private void GivenPublishedFunding()
        {
            _publishedFundingVersion = NewPublishedFundingVersion(_ => _.WithFundingId("funding1")
            .WithProviderFundings(new List<string> { "providerfunding1", "providerfunding2" })
            .WithFundingPeriod(_publishedFundingPeriod)
            .WithFundingStreamId("stream1")
            .WithOrganisationGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithOrganisationGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode)
            .WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
            .WithOrganisationGroupIdentifierValue("101"));

            _publishedFunding = NewPublishedFunding(_ => _.WithCurrent(_publishedFundingVersion));
        }

        private void GivenNewVersionCreated()
        {
            _publishedFundingVersionRepository.CreateVersion(Arg.Is(_publishedFundingVersion), Arg.Is(_publishedFunding.Current), Arg.Is(_publishedFunding.Current.PartitionKey))
                .Returns(_publishedFundingVersion);

        }

        private void GivenPublishedFundingUpserted()
        {

            _publishedFundingRepository.UpsertPublishedFunding(Arg.Is(_publishedFunding))
                .Returns(HttpStatusCode.OK);
        }

        private async Task WhenStatusUpdated()
        {
            await _publishedFundingStatusUpdateService.UpdatePublishedFundingStatus(new List<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)> { (_publishedFunding, _publishedFundingVersion) }, _author, PublishedFundingStatus.Released);
        }

        private void ThenExceptionShouldBeThrown(string message, Func<Task> invocation)
        {
            invocation
                .Should()
                .Throw<Exception>()
                .WithMessage(message);
        }

        private async Task ThenNewVersionSaved()
        {
            await _publishedFundingVersionRepository
                .Received(1)
                .SaveVersion(Arg.Is(_publishedFundingVersion), Arg.Is(_publishedFundingVersion.PartitionKey));
        }

        private PublishedFunding NewPublishedFunding(Action<PublishedFundingBuilder> setUp = null)
        {
            PublishedFundingBuilder publishedFundingBuilder = new PublishedFundingBuilder();

            setUp?.Invoke(publishedFundingBuilder);

            return publishedFundingBuilder.Build();
        }

        private PublishedFundingVersion NewPublishedFundingVersion(Action<PublishedFundingVersionBuilder> setUp = null)
        {
            PublishedFundingVersionBuilder publishedFundingVersionBuilder = new PublishedFundingVersionBuilder();

            setUp?.Invoke(publishedFundingVersionBuilder);

            return publishedFundingVersionBuilder.Build();
        }
    }
}
