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
    public class FundingStreamsWithDatasetsServiceTests
    {
        private Mock<IPublishingAreaRepository> _publishingArea;

        private FundingStreamsWithDatasetsService _service;

        [TestInitialize]
        public void SetUp()
        {
            _publishingArea = new Mock<IPublishingAreaRepository>();
            
            _service = new FundingStreamsWithDatasetsService(_publishingArea.Object);
        }

        [TestMethod]
        public async Task GetFundingStreamsWithDatasets()
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
            _publishingArea.Setup(_ => _.GetFundingStreamsWithDatasets())
                .ReturnsAsync(fundingStreams);
        }

        private async Task<IEnumerable<string>> WhenTheFundingStreamsAreQueried()
            => await _service.GetFundingStreamsWithDatasets();
        
        private string[] NewRandomStrings() => new[]
        {
            NewRandomString(), NewRandomString(), NewRandomString()
        };
        
        private string NewRandomString() => new RandomString();
    }
}