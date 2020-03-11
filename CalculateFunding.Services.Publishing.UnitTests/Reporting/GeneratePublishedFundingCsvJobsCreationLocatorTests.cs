using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Reporting;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting
{
    [TestClass]
    public class GeneratePublishedFundingCsvJobsCreationLocatorTests
    {
        private Mock<IGeneratePublishedFundingCsvJobsCreation> _transformOne;
        private Mock<IGeneratePublishedFundingCsvJobsCreation> _transformTwo;
        private Mock<IGeneratePublishedFundingCsvJobsCreation> _transformThree;

        private Mock<IGeneratePublishedFundingCsvJobsCreation>[] _transforms;

        private GeneratePublishedFundingCsvJobsCreationLocator _serviceLocator;

        [TestInitialize]
        public void SetUp()
        {
            _transformOne = new Mock<IGeneratePublishedFundingCsvJobsCreation>();
            _transformTwo = new Mock<IGeneratePublishedFundingCsvJobsCreation>();
            _transformThree = new Mock<IGeneratePublishedFundingCsvJobsCreation>();

            _transforms = new[]
            {
                _transformOne,
                _transformTwo,
                _transformThree
            };

            _serviceLocator = new GeneratePublishedFundingCsvJobsCreationLocator(_transforms.Select(_ => _.Object));
        }

        [TestMethod]
        public void ReturnsSingleMatchingTransformForJobType()
        {
            int supportedTransform = new RandomNumberBetween(0, 2);
            GeneratePublishingCsvJobsCreationAction action = new RandomEnum<GeneratePublishingCsvJobsCreationAction>();

            GivenTheTransformSupportsTheJobType(action, supportedTransform);

            _serviceLocator.GetService(action)
                .Should()
                .BeSameAs(_transforms[supportedTransform].Object);
        }

        [TestMethod]
        public void ThrowsArgumentOutOfRangeExceptionIfNoTransformForJobType()
        {
            Func<IGeneratePublishedFundingCsvJobsCreation> invocation = () => _serviceLocator.GetService(GeneratePublishingCsvJobsCreationAction.Undefined);

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>();
        }

        private void GivenTheTransformSupportsTheJobType(GeneratePublishingCsvJobsCreationAction action,
            int transformIndex)
        {
            _transforms[transformIndex].Setup(_ => _.IsForAction(action))
                .Returns(true);
        }
    }
}
