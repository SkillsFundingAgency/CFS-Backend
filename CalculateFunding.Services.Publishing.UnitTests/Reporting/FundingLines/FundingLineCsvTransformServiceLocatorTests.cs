using System;
using System.Linq;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    [TestClass]
    public class FundingLineCsvTransformServiceLocatorTests
    {
        private Mock<IFundingLineCsvTransform> _transformOne;
        private Mock<IFundingLineCsvTransform> _transformTwo;
        private Mock<IFundingLineCsvTransform> _transformThree;

        private Mock<IFundingLineCsvTransform>[] _transforms;

        private FundingLineCsvTransformServiceLocator _serviceLocator;

        [TestInitialize]
        public void SetUp()
        {
            _transformOne = new Mock<IFundingLineCsvTransform>();    
            _transformTwo = new Mock<IFundingLineCsvTransform>();    
            _transformThree = new Mock<IFundingLineCsvTransform>();

            _transforms = new[]
            {
                _transformOne,
                _transformTwo,
                _transformThree
            };
            
            _serviceLocator = new FundingLineCsvTransformServiceLocator(_transforms.Select(_ => _.Object));
        }

        [TestMethod]
        public void ReturnsSingleMatchingTransformForJobType()
        {
            int supportedTransform = new RandomNumberBetween(0, 2);
            FundingLineCsvGeneratorJobType jobType = new RandomEnum<FundingLineCsvGeneratorJobType>();
            
            GivenTheTransformSupportsTheJobType(jobType, supportedTransform);

            _serviceLocator.GetService(jobType)
                .Should()
                .BeSameAs(_transforms[supportedTransform].Object);
        }

        [TestMethod]
        public void ThrowsArgumentOutOfRangeExceptionIfNoTransformForJobType()
        {
            Func<IFundingLineCsvTransform> invocation = () => _serviceLocator.GetService(FundingLineCsvGeneratorJobType.Undefined);

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>();
        }

        private void GivenTheTransformSupportsTheJobType(FundingLineCsvGeneratorJobType jobType,
            int transformIndex)
        {
            _transforms[transformIndex].Setup(_ => _.IsForJobType(jobType))
                .Returns(true);
        }
    }
}