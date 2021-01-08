using System;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Errors;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
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
        private IErrorDetectionStrategyLocator _errorDetectionStrategyLocator;
        private PublishedProvidersContext _publishedProvidersContext;

        [TestInitialize]
        public void SetUp()
        {
            _detectorOne = NewDetectorMock();
            _detectorTwo = NewDetectorMock();
            _detectorThree = NewDetectorMock();

            _detectorOne.Setup(_ => _.Name).Returns(Guid.NewGuid().ToString());
            _detectorTwo.Setup(_ => _.Name).Returns(Guid.NewGuid().ToString());
            _detectorThree.Setup(_ => _.Name).Returns(Guid.NewGuid().ToString());

            _publishedProvidersContext = new PublishedProvidersContext();
            _publishedProvidersContext.FundingConfiguration = new Common.ApiClient.Policies.Models.FundingConfig.FundingConfiguration
            {
                ErrorDetectors = new[]
                {
                    _detectorOne.Object.Name,
                    _detectorTwo.Object.Name,
                    _detectorThree.Object.Name
                }
            };

            _errorDetectionStrategyLocator = new ErrorDetectionStrategyLocator(new[]
            {
                _detectorOne.Object,
                _detectorTwo.Object,
                _detectorThree.Object
            });

            _errorDetection = new PublishedProviderErrorDetection(_errorDetectionStrategyLocator);
        }

        [TestMethod]
        public async Task DelegatesToEachConfiguredErrorDetector()
        {
            PublishedProvider publishedProvider = NewPublishedProvider();

            publishedProvider.Current.AddErrors(new[]
            {
                new PublishedProviderError(),
                new PublishedProviderError(),
                new PublishedProviderError(),
                new PublishedProviderError()
            });

            
            await _errorDetection.ProcessPublishedProvider(publishedProvider, _publishedProvidersContext);

            _detectorOne.Verify(_ => _.DetectErrors(publishedProvider, _publishedProvidersContext), Times.Once);
            _detectorTwo.Verify(_ => _.DetectErrors(publishedProvider, _publishedProvidersContext), Times.Once);
            _detectorThree.Verify(_ => _.DetectErrors(publishedProvider, _publishedProvidersContext), Times.Once);
        }

        private PublishedProvider NewPublishedProvider() => new PublishedProviderBuilder()
            .Build();

        private Mock<IDetectPublishedProviderErrors> NewDetectorMock() => new Mock<IDetectPublishedProviderErrors>();
    }
}