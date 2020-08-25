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
    public class ProviderSnapshotMetadataRetrievalServiceTests
    {
        private Mock<IPublishingAreaRepository> _publishingArea;
        private Mock<IMapper> _mapper;

        private ProviderSnapshotMetadataRetrievalService _service;

        [TestInitialize]
        public void SetUp()
        {
            _publishingArea = new Mock<IPublishingAreaRepository>();
            _mapper = new Mock<IMapper>();
            
            _service = new ProviderSnapshotMetadataRetrievalService(_publishingArea.Object,
                _mapper.Object);
        }

        [TestMethod]
        public async Task GetProvidersSnapshotMetadata()
        {
            int snapShotId = NewRandomNumber();
            
            PublishingAreaProviderSnapshot publishingAreaProviderSnapshot = new PublishingAreaProviderSnapshot();
            ProviderSnapshot expectedSnapshot = new ProviderSnapshot();
            
            GivenTheProviderSnapshotMetadata(snapShotId, publishingAreaProviderSnapshot);
            AndTheMappedSnapshot(publishingAreaProviderSnapshot, expectedSnapshot);

            ProviderSnapshot actualMetadata = await WhenTheProvidersSnapShotMetadataIsQueried(snapShotId);

            actualMetadata
                .Should()
                .BeSameAs(expectedSnapshot);
        }

        private async Task<ProviderSnapshot> WhenTheProvidersSnapShotMetadataIsQueried(int snapShotId)
            => await _service.GetProviderSnapshotsMetadata(snapShotId);

        private void GivenTheProviderSnapshotMetadata(int snapShotId,
            PublishingAreaProviderSnapshot publishingAreaProviderSnapshot)
        {
            _publishingArea.Setup(_ => _.GetProviderSnapshotMetadata(snapShotId))
                .ReturnsAsync(publishingAreaProviderSnapshot);
        }

        private void AndTheMappedSnapshot(PublishingAreaProviderSnapshot publishingAreaProviderSnapshot,
            ProviderSnapshot providerSnapshot)
        {
            _mapper.Setup(_ => _.Map<ProviderSnapshot>(publishingAreaProviderSnapshot))
                .Returns(providerSnapshot);
        }

        private int NewRandomNumber() => new RandomNumberBetween(1, int.MaxValue);     
    }
}