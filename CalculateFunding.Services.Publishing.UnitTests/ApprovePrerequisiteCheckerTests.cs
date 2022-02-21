using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
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
        private IJobsRunning _jobsRunning;
        private ILogger _logger;
        private IJobManagement _jobManagement;
        private ApproveAllProvidersPrerequisiteChecker _approvePrerequisiteChecker;

        [TestInitialize]
        public void SetUp()
        {
            _jobsRunning = Substitute.For<IJobsRunning>();
            _jobManagement = Substitute.For<IJobManagement>();
            _logger = Substitute.For<ILogger>();

            _approvePrerequisiteChecker = new ApproveAllProvidersPrerequisiteChecker(
                _jobsRunning,
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
                    _.Message == $"Value cannot be null. (Parameter 'specificationId')");
        }

        [TestMethod]
        public void ReturnsErrorMessageWhenJobTypesRunning()
        {
            // Arrange
            string specificationId = "specId01";
            
            string errorMessage = $"{JobConstants.DefinitionNames.RefreshFundingJob} is still running";

            GivenCalculationEngineRunningStatusForTheSpecification(specificationId, JobConstants.DefinitionNames.RefreshFundingJob);
            
            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(specificationId);

            invocation
                .Should()
                .Throw<JobPrereqFailedException>()
                .Where(_ =>
                    _.Message == $"Approve All Providers with specification id: '{specificationId}' has prerequisites which aren't complete.");

            // Assert
            _logger
                .Received()
                .Error(errorMessage);
        }

        private void GivenCalculationEngineRunningStatusForTheSpecification(string specificationId, params string[] jobDefinitions)
        {
            _jobsRunning.GetJobTypes(specificationId, Arg.Any<string[]>())
                .Returns(jobDefinitions);
        }

        private async Task WhenThePreRequisitesAreChecked(string specificationId)
        {
            await _approvePrerequisiteChecker.PerformChecks(specificationId, null);
        }

    }
}
