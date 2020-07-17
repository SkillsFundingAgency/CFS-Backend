using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Errors;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.Errors
{
    [TestClass]
    public class PublishedProviderErrorDetectionTests
    {
        private Mock<IDetectPublishedProviderErrors> _detectorOne;
        private Mock<IDetectPublishedProviderErrors> _detectorTwo;
        private Mock<IDetectPublishedProviderErrors> _detectorThree;

        private PublishedProviderErrorDetection _errorDetection;

        [TestInitialize]
        public void SetUp()
        {
            _detectorOne = NewDetectorMock();
            _detectorTwo = NewDetectorMock();
            _detectorThree = NewDetectorMock();

            _errorDetection = new PublishedProviderErrorDetection(new[]
            {
                _detectorOne.Object,
                _detectorTwo.Object,
                _detectorThree.Object
            });
        }

        [TestMethod]
        public async Task ResetsErrorsOnPublishedProviderAndDelegatesToEachConfiguredErrorDetector()
        {
            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion();

            publishedProviderVersion.AddErrors(new[]
            {
                new PublishedProviderError(),
                new PublishedProviderError(),
                new PublishedProviderError(),
                new PublishedProviderError()
            });

            await _errorDetection.ProcessPublishedProvider(publishedProviderVersion);

            publishedProviderVersion.Errors
                .Should()
                .BeEmpty();

            _detectorOne.Verify(_ => _.DetectErrors(publishedProviderVersion), Times.Once);
            _detectorTwo.Verify(_ => _.DetectErrors(publishedProviderVersion), Times.Once);
            _detectorThree.Verify(_ => _.DetectErrors(publishedProviderVersion), Times.Once);
        }

        private PublishedProviderVersion NewPublishedProviderVersion() => new PublishedProviderVersionBuilder()
            .Build();
        private Mock<IDetectPublishedProviderErrors> NewDetectorMock() => new Mock<IDetectPublishedProviderErrors>();
    }
}