using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class ApprovePrerequisiteCheckerTests
    {
        private ICalculationEngineRunningChecker _calculationEngineRunningChecker;
        private ILogger _logger;
        private IJobManagement _jobManagement;
        private ApprovePrerequisiteChecker _approvePrerequisiteChecker;

        [TestInitialize]
        public void SetUp()
        {
            _calculationEngineRunningChecker = Substitute.For<ICalculationEngineRunningChecker>();
            _jobManagement = Substitute.For<IJobManagement>();
            _logger = Substitute.For<ILogger>();

            _approvePrerequisiteChecker = new ApprovePrerequisiteChecker(
                _calculationEngineRunningChecker,
                _jobManagement,
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
        public void ReturnsErrorMessageWhenCalculationEngineRunning()
        {
            // Arrange
            string specificationId = "specId01";
            
            string errorMessage = "Calculation engine is still running";

            GivenCalculationEngineRunningStatusForTheSpecification(specificationId, true);
            
            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(specificationId);

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{specificationId} has prerequisites which aren't complete.");

            // Assert
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
            await _approvePrerequisiteChecker.PerformChecks(specificationId, null);
        }

    }
}
