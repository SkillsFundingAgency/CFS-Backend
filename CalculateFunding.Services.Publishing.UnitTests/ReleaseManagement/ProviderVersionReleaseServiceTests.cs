using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class ProviderVersionReleaseServiceTests
    {
        private ProviderVersionReleaseService _service;
        private Mock<IReleaseToChannelSqlMappingContext> _releaseToChannelSqlMappingContext;
        private Mock<IReleaseManagementRepository> _releaseManagementRepository;
        private Mock<ILogger> _logger;
        private IEnumerable<PublishedProviderVersion> _providerVersions;
        private string _specificationId;

        [TestInitialize]
        public void Initialise()
        {
            _specificationId = new RandomString();
            _releaseToChannelSqlMappingContext = new Mock<IReleaseToChannelSqlMappingContext>();
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();
            _logger = new Mock<ILogger>();
            _service = new ProviderVersionReleaseService(_releaseToChannelSqlMappingContext.Object,
                _releaseManagementRepository.Object, _logger.Object);
        }

        [TestMethod]
        public async Task WhenGivenProviderVersionsNotInContextThenTheyAreSuccessfullyReleased()
        {
            PublishedProviderVersion[] providerVersions = new List<PublishedProviderVersion>
            {
                new PublishedProviderVersion
                {
                    ProviderId = new RandomString(),
                    FundingStreamId = new RandomString(),
                    FundingPeriodId = new RandomString(),
                    MajorVersion = 1,
                    MinorVersion = 2
                },
                new PublishedProviderVersion
                {
                    ProviderId = new RandomString(),
                    FundingStreamId = new RandomString(),
                    FundingPeriodId = new RandomString(),
                    MajorVersion = 1,
                    MinorVersion = 2
                },
            }.ToArray();

            PublishedProviderVersion[] providerVersionsInContext = new List<PublishedProviderVersion>().ToArray();

            GivenProviderVersions(providerVersions);
            GivenReleasedProvidersInContext(providerVersions);
            GivenReleasedProviderVersionsInContext(providerVersionsInContext);

            await WhenProviderVersionsReleased();

            ThenProvidersAreReleased(providerVersions);
            AndTheContextIsUpdated(providerVersions);
        }

        [TestMethod]
        public void WhenProviderMissingInContextKeyNotFoundExceptionThrown()
        {
            PublishedProviderVersion[] providerVersions = new List<PublishedProviderVersion>
            {
                new PublishedProviderVersion
                {
                    ProviderId = new RandomString(),
                    FundingStreamId = new RandomString(),
                    FundingPeriodId = new RandomString(),
                    MajorVersion = 1,
                    MinorVersion = 2
                },
                new PublishedProviderVersion
                {
                    ProviderId = new RandomString(),
                    FundingStreamId = new RandomString(),
                    FundingPeriodId = new RandomString(),
                    MajorVersion = 1,
                    MinorVersion = 2
                },
            }.ToArray();

            PublishedProviderVersion[] providerVersionsInContext = new List<PublishedProviderVersion>().ToArray();

            GivenProviderVersions(providerVersions);
            GivenReleasedProvidersInContext(providerVersions.First());
            GivenReleasedProviderVersionsInContext(providerVersionsInContext);

            Func<Task> result = async () => await WhenProviderVersionsReleased();

            result
                .Should()
                .ThrowExactly<KeyNotFoundException>();
        }

        private void GivenProviderVersions(params PublishedProviderVersion[] providerVersions)
        {
            _providerVersions = providerVersions;
        }

        private void GivenReleasedProvidersInContext(params PublishedProviderVersion[] providersInContext)
        {
            ReleasedProvider[] releasedProviders = providersInContext.Select(s => new ReleasedProvider
            {
                ProviderId = s.ProviderId,
                SpecificationId = _specificationId
            }).ToArray();

            _releaseToChannelSqlMappingContext.SetupGet(_ => _.ReleasedProviders)
                .Returns(releasedProviders.ToDictionary(_ => _.ProviderId));
        }

        private async Task WhenProviderVersionsReleased()
        {
            await _service.ReleaseProviderVersions(_providerVersions, _specificationId);
        }

        private void GivenReleasedProviderVersionsInContext(params PublishedProviderVersion[] providerVersionsInContext)
        {
            Dictionary<string, string> providerIdLookup = _providerVersions.ToDictionary(_ => _.FundingId, _ => _.ProviderId);

            ReleasedProviderVersion[] releasedProviderVersions = providerVersionsInContext.Select(s => new ReleasedProviderVersion
            {
                FundingId = s.FundingId,
                MajorVersion = s.MajorVersion,
                MinorVersion = s.MinorVersion,
                ReleasedProviderId = new RandomNumberBetween(1, 1000)
            }).ToArray();

            _releaseToChannelSqlMappingContext.SetupGet(_ => _.ReleasedProviderVersions)
                .Returns(releasedProviderVersions.ToDictionary(_ => providerIdLookup[_.FundingId]));
        }

        private void ThenProvidersAreReleased(params PublishedProviderVersion[] providerVersions)
        {
            _releaseManagementRepository.Verify(_ =>
                _.CreateReleasedProviderVersionsUsingAmbientTransaction(It.IsAny<ReleasedProviderVersion>()), Times.Exactly(providerVersions.Count()));
        }

        private void AndTheContextIsUpdated(params PublishedProviderVersion[] providers)
        {
            _releaseToChannelSqlMappingContext.Object.ReleasedProviderVersions.Select(_ => _.Key).SequenceEqual(providers.Select(s => s.ProviderId));
        }
    }
}
