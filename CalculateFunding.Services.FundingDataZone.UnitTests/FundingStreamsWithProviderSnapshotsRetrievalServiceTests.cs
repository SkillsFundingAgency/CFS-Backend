using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.FundingDataZone.UnitTests
{
    [TestClass]
    public class FundingStreamsWithProviderSnapshotsRetrievalServiceTests
    {
        private Mock<IPublishingAreaRepository> _publishingArea;

        private FundingStreamsWithProviderSnapshotsRetrievalService _service;

        [TestInitialize]
        public void SetUp()
        {
            _publishingArea = new Mock<IPublishingAreaRepository>();
            
            _service = new FundingStreamsWithProviderSnapshotsRetrievalService(_publishingArea.Object);
        }

        [TestMethod]
        public async Task GetFundingStreamsWithProviderSnapshots()
        {
            string[] expectedFundingStreams = NewRandomStrings();
            
            GivenTheFundingStreamsWithDatasets(expectedFundingStreams);

            IEnumerable<string> actualFundingStreams = await WhenTheFundingStreamsAreQueried();

            actualFundingStreams
                .Should()
                .BeEquivalentTo(expectedFundingStreams);
        }

        private void GivenTheFundingStreamsWithDatasets(IEnumerable<string> fundingStreams)
        {
            _publishingArea.Setup(_ => _.GetFundingStreamsWithProviderSnapshots())
                .ReturnsAsync(fundingStreams);
        }

        private async Task<IEnumerable<string>> WhenTheFundingStreamsAreQueried()
            => await _service.GetFundingStreamsWithProviderSnapshots();
        
        private string[] NewRandomStrings() => new[]
        {
            NewRandomString(), NewRandomString(), NewRandomString()
        };
        
        private string NewRandomString() => new RandomString();
    }
}