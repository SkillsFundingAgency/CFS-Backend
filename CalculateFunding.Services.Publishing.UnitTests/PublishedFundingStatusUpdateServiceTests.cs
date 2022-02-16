using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedFundingStatusUpdateServiceTests
    {
        private PublishedFundingStatusUpdateService _publishedFundingStatusUpdateService;
        private IPublishedFundingBulkRepository _publishedFundingBulkRepository;
        private IVersionRepository<PublishedFundingVersion> _publishedFundingVersionRepository;
        private IVersionBulkRepository<PublishedFundingVersion> _publishedFundingVersionBulkRepository;
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
            _publishedFundingBulkRepository = Substitute.For<IPublishedFundingBulkRepository>();
            _publishedFundingVersionRepository = Substitute.For<IVersionRepository<PublishedFundingVersion>>();
            _publishedFundingVersionBulkRepository = Substitute.For<IVersionBulkRepository<PublishedFundingVersion>>();
            _publishedFundingIdGeneratorResolver = Substitute.For<IPublishedFundingIdGeneratorResolver>();
            _author = new Reference { Id = "authorId", Name = "author" };

            _publishedFundingStatusUpdateService = new PublishedFundingStatusUpdateService(PublishingResilienceTestHelper.GenerateTestPolicies(),
                                                                                           _publishedFundingVersionRepository,
                                                                                           _publishedFundingIdGeneratorResolver,
                                                                                           logger,
                                                                                           new PublishingEngineOptions(configuration),
                                                                                           _publishedFundingVersionBulkRepository,
                                                                                           _publishedFundingBulkRepository);

            _publishedFundingPeriod = new PublishedFundingPeriod { Id = $"{PublishedFundingPeriodType.AY}-123", Type = PublishedFundingPeriodType.AY, Period = "123" };
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
        public async Task UpdatePublishedFundingStatus_GivenExistingPublishedFundingAndNewPublishedFundingVersionWithJobIdanCorrelationIdAndSaveSucceeds_NewVersionSaved()
        {
            GivenPublishedFunding();

            GivenNewVersionCreated();

            GivenPublishedFundingUpserted();

            await WhenStatusUpdated();

            await ThenNewVersionSaved();
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
            IEnumerable<PublishedFunding> publishedFundings = new List<PublishedFunding> { _publishedFunding };

            _publishedFundingBulkRepository
                .UpsertPublishedFundings(
                    Arg.Is<IEnumerable<PublishedFunding>>(_ => _.SequenceEqual(publishedFundings)),
                    Arg.Any<Action<Task<HttpStatusCode>, PublishedFunding>>())
                .Returns(Task.FromResult(HttpStatusCode.OK));
        }

        private async Task WhenStatusUpdated()
        {
            await _publishedFundingStatusUpdateService.UpdatePublishedFundingStatus(new List<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)> { (_publishedFunding, _publishedFundingVersion) }, PublishedFundingStatus.Released);
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
            await _publishedFundingVersionBulkRepository
                .Received(1)
                .SaveVersion(
                    Arg.Is(_publishedFundingVersion),
                    Arg.Is(_publishedFundingVersion.PartitionKey));
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
