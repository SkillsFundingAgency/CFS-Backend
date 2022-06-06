using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.FundingDataZone.UnitTests
{
    [TestClass]
    public class ProviderSnapshotFundingPeriodServiceTests
    {
        private Mock<IPublishingAreaRepository> _publishingArea;

        private ProviderSnapshotFundingPeriodService _service;

        [TestInitialize]
        public void SetUp()
        {
            _publishingArea = new Mock<IPublishingAreaRepository>();

            _service = new ProviderSnapshotFundingPeriodService(_publishingArea.Object);
        }

        [TestMethod]
        public async Task PopulateFundingPeriods()
        {
            await WhenPopulateFundingPeriods();

            _publishingArea.Verify(_ => _.PopulateFundingPeriods(), Times.Once);
        }

        [TestMethod]
        public async Task PopulateFundingPeriod()
        {
            int providerSnapshotId = NewRandomNumber();

            await WhenPopulateFundingPeriod(providerSnapshotId);

            _publishingArea.Verify(_ => _.PopulateFundingPeriod(providerSnapshotId), Times.Once);
        }

        private async Task WhenPopulateFundingPeriods() => await _service.PopulateFundingPeriods();

        private async Task WhenPopulateFundingPeriod(int providerSnapshotId) => await _service.PopulateFundingPeriod(providerSnapshotId);

        private int NewRandomNumber() => new RandomNumberBetween(1, int.MaxValue);
    }
}
