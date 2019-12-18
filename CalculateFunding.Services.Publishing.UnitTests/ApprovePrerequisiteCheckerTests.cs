using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class ApprovePrerequisiteCheckerTests
    {
        private ICalculationEngineRunningChecker _calculationEngineRunningChecker;
        private ILogger _logger;
        private ApprovePrerequisiteChecker _approvePrerequisiteChecker;

        private IEnumerable<string> _validationErrors;

        [TestInitialize]
        public void SetUp()
        {
            _calculationEngineRunningChecker = Substitute.For<ICalculationEngineRunningChecker>();
            _logger = Substitute.For<ILogger>();

            _approvePrerequisiteChecker = new ApprovePrerequisiteChecker(
                _calculationEngineRunningChecker,
                _logger);
        }

        [TestMethod]
        public void ThrowsArgumentNullException()
        {
            // Arrange

            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(null);

            // Assert
            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Where(_ =>
                    _.Message == $"Value cannot be null.{Environment.NewLine}Parameter name: specificationId");
        }

        [TestMethod]
        public async Task ReturnsErrorMessageWhenCalculationEngineRunning()
        {
            // Arrange
            string specificationId = "specId01";
            
            string errorMessage = "Calculation engine is still running";

            GivenCalculationEngineRunningStatusForTheSpecification(specificationId, true);
            
            // Act
            await WhenThePreRequisitesAreChecked(specificationId);

            // Assert
            _validationErrors
                .Should()
                .Contain(new[]
                {
                    errorMessage
                });

            _logger
                .Received()
                .Error(errorMessage);
        }

        private void GivenCalculationEngineRunningStatusForTheSpecification(string specificationId, bool calculationEngineRunningStatus)
        {
            _calculationEngineRunningChecker.IsCalculationEngineRunning(specificationId, Arg.Any<string[]>())
                .Returns(calculationEngineRunningStatus);
        }

        private async Task WhenThePreRequisitesAreChecked(string specificationId)
        {
            _validationErrors = await _approvePrerequisiteChecker.PerformPrerequisiteChecks(specificationId);
        }

    }
}
