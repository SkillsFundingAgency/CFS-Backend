using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Errors;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;
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
        private Mock<IDetectPublishedProviderErrors> _detectorFour;
        private Mock<IDetectPublishedProviderErrors> _detectorFive;

        private PublishedProviderErrorDetection _errorDetection;
        private IErrorDetectionStrategyLocator _errorDetectionStrategyLocator;
        private PublishedProvidersContext _publishedProvidersContext;

        [TestInitialize]
        public void SetUp()
        {
            _detectorOne = NewDetectorMock(true, false, false, false);
            _detectorTwo = NewDetectorMock(false, true, false, false);
            _detectorThree = NewDetectorMock(false, false, true, false);
            _detectorFour = NewDetectorMock(false, true, true, true);
            _detectorFive = NewDetectorMock(false, true, true, true);

            _publishedProvidersContext = new PublishedProvidersContext
            {
                FundingConfiguration = new FundingConfiguration
                {
                    ErrorDetectors = new[]
                    {
                        _detectorOne.Object.Name,
                        _detectorTwo.Object.Name,
                        _detectorThree.Object.Name,
                    }
                }
            };

            _errorDetectionStrategyLocator = new ErrorDetectionStrategyLocator(new[]
            {
                _detectorOne.Object,
                _detectorTwo.Object,
                _detectorThree.Object,
                _detectorFour.Object,
                _detectorFive.Object
            });

            _errorDetection = new PublishedProviderErrorDetection(_errorDetectionStrategyLocator);
        }

        [TestMethod]
        public async Task ErrorDetectorsReturnedInCorrectOrder()
        {
            _detectorFive.SetupGet(_ => _.RunningOrder).Returns(1);

            IEnumerable<IDetectPublishedProviderErrors> errorDetectors = _errorDetectionStrategyLocator.GetErrorDetectorsForAllFundingConfigurations();

            errorDetectors
                .First()
                .Should()
                .Be(_detectorFive.Object);
        }

        [TestMethod]
        public async Task ApplyRefreshPreVariationErrorDetectionDelegatesToEachConfiguredPreVariationErrorDetector()
        {
            PublishedProvider publishedProvider = NewPublishedProvider();
            
            await _errorDetection.ApplyRefreshPreVariationErrorDetection(publishedProvider, _publishedProvidersContext);

            _detectorOne.Verify(_ => _.DetectErrors(publishedProvider, _publishedProvidersContext), Times.Once);
            _detectorTwo.Verify(_ => _.DetectErrors(publishedProvider, _publishedProvidersContext), Times.Never);
            _detectorThree.Verify(_ => _.DetectErrors(publishedProvider, _publishedProvidersContext), Times.Never);
            _detectorFour.Verify(_ => _.DetectErrors(publishedProvider, _publishedProvidersContext), Times.Never);
        }
        
        [TestMethod]
        public async Task ApplyRefreshPostVariationErrorDetectionDelegatesToEachConfiguredPostVariationErrorDetector()
        {
            _detectorFive.SetupGet(_ => _.RunningOrder).Returns(1);

            PublishedProvider publishedProvider = NewPublishedProvider();
            
            await _errorDetection.ApplyRefreshPostVariationsErrorDetection(publishedProvider, _publishedProvidersContext);

            _detectorOne.Verify(_ => _.DetectErrors(publishedProvider, _publishedProvidersContext), Times.Never);
            _detectorTwo.Verify(_ => _.DetectErrors(publishedProvider, _publishedProvidersContext), Times.Once);
            _detectorThree.Verify(_ => _.DetectErrors(publishedProvider, _publishedProvidersContext), Times.Never);
            _detectorFour.Verify(_ => _.DetectErrors(publishedProvider, _publishedProvidersContext), Times.Once);
        }
        
        [TestMethod]
        public async Task ApplyAssignProfilePatternErrorDetectionDelegatesToEachConfiguredAssignProfilePatternErrorDetector()
        {
            PublishedProvider publishedProvider = NewPublishedProvider();
            
            await _errorDetection.ApplyAssignProfilePatternErrorDetection(publishedProvider, _publishedProvidersContext);

            _detectorOne.Verify(_ => _.DetectErrors(publishedProvider, _publishedProvidersContext), Times.Never);
            _detectorTwo.Verify(_ => _.DetectErrors(publishedProvider, _publishedProvidersContext), Times.Never);
            _detectorThree.Verify(_ => _.DetectErrors(publishedProvider, _publishedProvidersContext), Times.Once);
            _detectorFour.Verify(_ => _.DetectErrors(publishedProvider, _publishedProvidersContext), Times.Once);
        }

        private PublishedProvider NewPublishedProvider() => new PublishedProviderBuilder()
            .Build();

        private Mock<IDetectPublishedProviderErrors> NewDetectorMock(bool isPreVariation,
            bool isPostVariation,
            bool isAssignProfilePatternCheck,
            bool isForAllFundingConfigurations)
        {
            Mock<IDetectPublishedProviderErrors> detector = new Mock<IDetectPublishedProviderErrors>();

            detector.Setup(_ => _.Name)
                .Returns(NewRandomString());
            detector.Setup(_ => _.IsPostVariationCheck)
                .Returns(isPostVariation);
            detector.Setup(_ => _.IsPreVariationCheck)
                .Returns(isPreVariation);
            detector.Setup(_ => _.IsForAllFundingConfigurations)
                .Returns(isForAllFundingConfigurations);
            detector.Setup(_ => _.IsAssignProfilePatternCheck)
                .Returns(isAssignProfilePatternCheck);
            
            return detector;
        }

        private string NewRandomString() => new RandomString();
    }
}