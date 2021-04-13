using CalculateFunding.Services.Users.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace CalculateFunding.Services.Users
{
    [TestClass]
    public class FundingStreamPermissionsUsersCsvTransformServiceLocatorTests
    {
        private Mock<IUsersCsvTransform> _transformOne;
        private Mock<IUsersCsvTransform> _transformTwo;
        private Mock<IUsersCsvTransform> _transformThree;

        private Mock<IUsersCsvTransform>[] _transforms;

        private FundingStreamPermissionsUsersCsvTransformServiceLocator _serviceLocator;

        [TestInitialize]
        public void SetUp()
        {
            _transformOne = new Mock<IUsersCsvTransform>();
            _transformTwo = new Mock<IUsersCsvTransform>();
            _transformThree = new Mock<IUsersCsvTransform>();

            _transforms = new[]
            {
                _transformOne,
                _transformTwo,
                _transformThree
            };

            _serviceLocator = new FundingStreamPermissionsUsersCsvTransformServiceLocator(_transforms.Select(_ => _.Object));
        }

        [TestMethod]
        public void ReturnsSingleMatchingTransformForJobType()
        {
            int supportedTransform = new RandomNumberBetween(0, 2);
            string jobDefinition = new RandomString();

            GivenTheTransformSupportsTheJobType(jobDefinition, supportedTransform);

            _serviceLocator.GetService(jobDefinition)
                .Should()
                .BeSameAs(_transforms[supportedTransform].Object);
        }

        [TestMethod]
        public void ThrowsArgumentOutOfRangeExceptionIfNoTransformForJobType()
        {
            Func<IUsersCsvTransform> invocation = () => _serviceLocator.GetService("n/a");

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>();
        }

        private void GivenTheTransformSupportsTheJobType(string jobDefinition,
            int transformIndex)
        {
            _transforms[transformIndex].Setup(_ => _.IsForJobDefinition(jobDefinition))
                .Returns(true);
        }
    }
}
