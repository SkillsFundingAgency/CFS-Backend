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
    public class FundingLineCsvBatchProcessorServiceLocatorTests
    {
        private Mock<IFundingLineCsvBatchProcessor> _batchProcessorOne;
        private Mock<IFundingLineCsvBatchProcessor> _batchProcessorTwo;

        private Mock<IFundingLineCsvBatchProcessor>[] _batchProcessors;

        private FundingLineCsvBatchProcessorServiceLocator _serviceLocator;

        [TestInitialize]
        public void SetUp()
        {
            _batchProcessorOne = new Mock<IFundingLineCsvBatchProcessor>();    
            _batchProcessorTwo = new Mock<IFundingLineCsvBatchProcessor>();    

            _batchProcessors = new[]
            {
                _batchProcessorOne,
                _batchProcessorTwo
            };
            
            _serviceLocator = new FundingLineCsvBatchProcessorServiceLocator(_batchProcessors.Select(_ => _.Object));
        }

        [TestMethod]
        public void ReturnsSingleMatchingTransformForJobType()
        {
            int supportedTransform = new RandomNumberBetween(0, 2);
            FundingLineCsvGeneratorJobType jobType = new RandomEnum<FundingLineCsvGeneratorJobType>();
            
            GivenTheTransformSupportsTheJobType(jobType, supportedTransform);

            _serviceLocator.GetService(jobType)
                .Should()
                .BeSameAs(_batchProcessors[supportedTransform].Object);
        }

        [TestMethod]
        public void ThrowsArgumentOutOfRangeExceptionIfNoTransformForJobType()
        {
            Func<IFundingLineCsvBatchProcessor> invocation = () => _serviceLocator.GetService(FundingLineCsvGeneratorJobType.Undefined);

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>();
        }

        private void GivenTheTransformSupportsTheJobType(FundingLineCsvGeneratorJobType jobType,
            int transformIndex)
        {
            _batchProcessors[transformIndex].Setup(_ => _.IsForJobType(jobType))
                .Returns(true);
        }  
    }
}