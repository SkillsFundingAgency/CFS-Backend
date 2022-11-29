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
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();


            IEnumerable<PublishingAreaProviderSnapshot> publishingAreaProviderSnapShots = ArraySegment<PublishingAreaProviderSnapshot>.Empty;
            IEnumerable<ProviderSnapshot> expectedProviderSnapShots = ArraySegment<ProviderSnapshot>.Empty;
            
            GivenThePublishingAreaProviderSnapShots(fundingStreamId, fundingPeriodId,publishingAreaProviderSnapShots);
            AndTheMappedProviderSnapShots(publishingAreaProviderSnapShots, expectedProviderSnapShots);

            IEnumerable<ProviderSnapshot> actualProviderSnapShots = await WhenTheProvidersSnapShotsAreQueried(fundingStreamId, fundingPeriodId);

            actualProviderSnapShots
                .Should()
                .BeSameAs(expectedProviderSnapShots);
        }

        [TestMethod]
        public async Task GetLatestProviderSnapshotsForAllFundingStreams()
        {
            IEnumerable<PublishingAreaProviderSnapshot> publishingAreaProviderSnapShots = ArraySegment<PublishingAreaProviderSnapshot>.Empty;
            IEnumerable<ProviderSnapshot> expectedProviderSnapShots = ArraySegment<ProviderSnapshot>.Empty;

            GivenTheLatestPublishingAreaProviderSnapShotsForFundingStreams(publishingAreaProviderSnapShots);
            AndTheMappedProviderSnapShots(publishingAreaProviderSnapShots, expectedProviderSnapShots);

            IEnumerable<ProviderSnapshot> actualProviderSnapShots = await WhenTheLatestProvidersSnapShotsAreQueried();

            actualProviderSnapShots
                .Should()
                .BeSameAs(expectedProviderSnapShots);
        }

        private async Task<IEnumerable<ProviderSnapshot>> WhenTheProvidersSnapShotsAreQueried(string fundingStreamId,string fundingPeriodId)
            => await _service.GetProviderSnapshotsForFundingStream(fundingStreamId, fundingPeriodId);

        private async Task<IEnumerable<ProviderSnapshot>> WhenTheLatestProvidersSnapShotsAreQueried()
           => await _service.GetLatestProviderSnapshotsForAllFundingStreams();

        private void GivenThePublishingAreaProviderSnapShots(string fundingStreamId,string fundingPeriodId,
            IEnumerable<PublishingAreaProviderSnapshot> publishingAreaProviderSnapShots)
        {
            _publishingArea.Setup(_ => _.GetProviderSnapshots(fundingStreamId, fundingPeriodId))
                .ReturnsAsync(publishingAreaProviderSnapShots);
        }

        private void GivenTheLatestPublishingAreaProviderSnapShotsForFundingStreams(IEnumerable<PublishingAreaProviderSnapshot> publishingAreaProviderSnapShots)
        {
            _publishingArea.Setup(_ => _.GetLatestProviderSnapshotsForAllFundingStreams())
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
}