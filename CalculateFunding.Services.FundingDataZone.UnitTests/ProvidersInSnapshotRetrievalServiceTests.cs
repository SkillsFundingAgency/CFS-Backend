using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.FundingDataZone.UnitTests
{
    [TestClass]
    public class ProviderSnapshotForFundingStreamServiceTests
    {
        private Mock<IPublishingAreaRepository> _publishingArea;
        private Mock<IMapper> _mapper;

        private ProviderSnapshotForFundingStreamService _service;

        [TestInitialize]
        public void SetUp()
        {
            _publishingArea = new Mock<IPublishingAreaRepository>();
            _mapper = new Mock<IMapper>();
            
            _service = new ProviderSnapshotForFundingStreamService(_publishingArea.Object,
                _mapper.Object);
        }

        [TestMethod]
        public async Task GetProviderSnapshotsForFundingStream()
        {
            string snapShotId = NewRandomString();
            
            IEnumerable<PublishingAreaProviderSnapshot> publishingAreaProviderSnapShots = ArraySegment<PublishingAreaProviderSnapshot>.Empty;
            IEnumerable<ProviderSnapshot> expectedProviderSnapShots = ArraySegment<ProviderSnapshot>.Empty;
            
            GivenThePublishingAreaProviderSnapShots(snapShotId, publishingAreaProviderSnapShots);
            AndTheMappedProviderSnapShots(publishingAreaProviderSnapShots, expectedProviderSnapShots);

            IEnumerable<ProviderSnapshot> actualProviderSnapShots = await WhenTheProvidersSnapShotsAreQueried(snapShotId);

            actualProviderSnapShots
                .Should()
                .BeSameAs(expectedProviderSnapShots);
        }

        private async Task<IEnumerable<ProviderSnapshot>> WhenTheProvidersSnapShotsAreQueried(string fundingStreamId)
            => await _service.GetProviderSnapshotsForFundingStream(fundingStreamId);

        private void GivenThePublishingAreaProviderSnapShots(string fundingStreamId,
            IEnumerable<PublishingAreaProviderSnapshot> publishingAreaProviderSnapShots)
        {
            _publishingArea.Setup(_ => _.GetProviderSnapshots(fundingStreamId))
                .ReturnsAsync(publishingAreaProviderSnapShots);
        }

        private void AndTheMappedProviderSnapShots(IEnumerable<PublishingAreaProviderSnapshot> publishingAreaProviderSnapShots,
            IEnumerable<ProviderSnapshot> providerSnapShots)
        {
            _mapper.Setup(_ => _.Map<IEnumerable<ProviderSnapshot>>(publishingAreaProviderSnapShots))
                .Returns(providerSnapShots);
        }

        private string NewRandomString() => new RandomString();
    }
    
    [TestClass]
    public class ProvidersInSnapshotRetrievalServiceTests
    {
        private Mock<IPublishingAreaRepository> _publishingArea;
        private Mock<IMapper> _mapper;

        private ProvidersInSnapshotRetrievalService _service;

        [TestInitialize]
        public void SetUp()
        {
            _publishingArea = new Mock<IPublishingAreaRepository>();
            _mapper = new Mock<IMapper>();
            
            _service = new ProvidersInSnapshotRetrievalService(_publishingArea.Object,
                _mapper.Object);
        }

        [TestMethod]
        public async Task GetProvidersInSnapshot()
        {
            int snapShotId = NewRandomNumber();
            
            IEnumerable<PublishingAreaProvider> publishingAreaProviders = ArraySegment<PublishingAreaProvider>.Empty;
            IEnumerable<Provider> expectedProviders = ArraySegment<Provider>.Empty;
            
            GivenThePublishingAreaProviders(snapShotId, publishingAreaProviders);
            AndTheMappedProviders(publishingAreaProviders, expectedProviders);

            IEnumerable<Provider> actualProviders = await WhenTheProvidersForSnapShotAreQueried(snapShotId);

            actualProviders
                .Should()
                .BeSameAs(expectedProviders);
        }

        private async Task<IEnumerable<Provider>> WhenTheProvidersForSnapShotAreQueried(int snapShotId)
            => await _service.GetProvidersInSnapshot(snapShotId);

        private void GivenThePublishingAreaProviders(int snapShotId,
            IEnumerable<PublishingAreaProvider> publishingAreaProviders)
        {
            _publishingArea.Setup(_ => _.GetProvidersInSnapshot(snapShotId))
                .ReturnsAsync(publishingAreaProviders);
        }

        private void AndTheMappedProviders(IEnumerable<PublishingAreaProvider> publishingAreaProviders,
            IEnumerable<Provider> providers)
        {
            _mapper.Setup(_ => _.Map<IEnumerable<Provider>>(publishingAreaProviders))
                .Returns(providers);
        }

        private int NewRandomNumber() => new RandomNumberBetween(1, int.MaxValue);
    }
}