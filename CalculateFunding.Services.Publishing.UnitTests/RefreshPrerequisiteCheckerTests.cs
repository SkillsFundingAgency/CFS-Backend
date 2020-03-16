using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
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
    public class RefreshPrerequisiteCheckerTests
    {
        private ISpecificationFundingStatusService _specificationFundingStatusService;
        private ISpecificationService _specificationService;
        private IJobsRunning _jobsRunning;
        private ICalculationPrerequisiteCheckerService _calculationApprovalCheckerService;
        private IJobManagement _jobManagement;
        private ILogger _logger;
        private RefreshPrerequisiteChecker _refreshPrerequisiteChecker;

        private IEnumerable<string> _validationErrors;

        [TestInitialize]
        public void SetUp()
        {
            _specificationFundingStatusService = Substitute.For<ISpecificationFundingStatusService>();
            _specificationService = Substitute.For<ISpecificationService>();
            _jobsRunning = Substitute.For<IJobsRunning>();
            _calculationApprovalCheckerService = Substitute.For<ICalculationPrerequisiteCheckerService>();
            _jobManagement = Substitute.For<IJobManagement>();
            _logger = Substitute.For<ILogger>();

            _refreshPrerequisiteChecker = new RefreshPrerequisiteChecker(
                _specificationFundingStatusService, 
                _specificationService,
                _jobsRunning,
                _calculationApprovalCheckerService,
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
                    _.Message == $"Value cannot be null.{Environment.NewLine}Parameter name: specification");
        }

        [TestMethod]
        public void ReturnsErrorMessageWhenSharesAlreadyChoseFundingStream()
        {
            // Arrange
            string specificationId = "specId01";
            SpecificationSummary specificationSummary = new SpecificationSummary { Id = specificationId };

            string errorMessage = $"Specification with id: '{specificationId} already shares chosen funding streams";
            
            GivenTheSpecificationFundingStatusForTheSpecification(specificationSummary, SpecificationFundingStatus.SharesAlreadyChosenFundingStream);

            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(specificationSummary);

            // Assert
            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{specificationSummary.Id} has prerequisites which aren't complete.");

            _logger
                .Received()
                .Error(errorMessage);

        }

        [TestMethod]
        public void ReturnsErrorMessageWhenCanChooseSpecificationFundingAndErrorSelectingSpecificationForFunding()
        {
            // Arrange
            string specificationId = "specId01";
            SpecificationSummary specificationSummary = new SpecificationSummary { Id = specificationId };

            string errorMessage = "Generic error message";

            GivenTheSpecificationFundingStatusForTheSpecification(specificationSummary, SpecificationFundingStatus.CanChoose);
            GivenExceptionThrownForSelectSpecificationForFunding(specificationId, new Exception(errorMessage));

            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(specificationSummary);

            // Assert
            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{specificationSummary.Id} has prerequisites which aren't complete.");

            _logger
                .Received()
                .Error(errorMessage);
        }

        [TestMethod]
        public void ReturnsErrorMessageWhenJobsRunning()
        {
            // Arrange
            string specificationId = "specId01";
            SpecificationSummary specificationSummary = new SpecificationSummary { Id = specificationId };

            string errorMessage = $"{JobConstants.DefinitionNames.CreateInstructAllocationJob} is still running";

            GivenTheSpecificationFundingStatusForTheSpecification(specificationSummary, SpecificationFundingStatus.AlreadyChosen);
            GivenCalculationEngineRunningStatusForTheSpecification(specificationId, JobConstants.DefinitionNames.CreateInstructAllocationJob);
            GivenValidationErrorsForTheSpecification(specificationSummary, Enumerable.Empty<string>());

            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(specificationSummary);

            // Assert
            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{specificationSummary.Id} has prerequisites which aren't complete.");

            _logger
                .Received()
                .Error(errorMessage);
        }

        [TestMethod]
        public void ReturnsErrorMessageWhenCalculationPrequisitesNotMet()
        {
            // Arrange
            string specificationId = "specId01";
            SpecificationSummary specificationSummary = new SpecificationSummary { Id = specificationId };

            string errorMessage = "Error message";

            GivenTheSpecificationFundingStatusForTheSpecification(specificationSummary, SpecificationFundingStatus.AlreadyChosen);
            GivenCalculationEngineRunningStatusForTheSpecification(specificationId);
            GivenValidationErrorsForTheSpecification(specificationSummary, new List<string> { errorMessage });

            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(specificationSummary);

            // Assert
            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{specificationSummary.Id} has prerequisites which aren't complete.");

            _logger
                .Received()
                .Error(errorMessage);
        }

        private void GivenTheSpecificationFundingStatusForTheSpecification(SpecificationSummary specification, SpecificationFundingStatus specificationFundingStatus)
        {
            _specificationFundingStatusService.CheckChooseForFundingStatus(specification)
                .Returns(specificationFundingStatus);
        }

        private void GivenExceptionThrownForSelectSpecificationForFunding(string specificationId, Exception ex)
        {
            _specificationService.SelectSpecificationForFunding(specificationId)
                .Throws(ex);
        }

        private void GivenCalculationEngineRunningStatusForTheSpecification(string specificationId, params string[] jobDefinitions)
        {
            _jobsRunning.GetJobTypes(specificationId, Arg.Any<string[]>())
                .Returns(jobDefinitions);
        }

        private void GivenValidationErrorsForTheSpecification(SpecificationSummary specification, IEnumerable<string> validationErrors)
        {
            _calculationApprovalCheckerService.VerifyCalculationPrerequisites(specification)
                .Returns(validationErrors);
        }

        private async Task WhenThePreRequisitesAreChecked(SpecificationSummary specification)
        {
            await _refreshPrerequisiteChecker.PerformChecks(specification, null);
        }

    }
}
