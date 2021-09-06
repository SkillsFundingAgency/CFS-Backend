using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.UnitTests
{
    [TestClass]
    public class CalculationEngineRunningCheckerTests

    {
        private IJobManagement _jobs;
        private JobSummary _job;
        private string _specificationId;
        private string[] _jobTypes;

        private CalculationEngineRunningChecker _calculationEngineRunningChecker;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = Substitute.For<IJobManagement>();
            _specificationId = NewRandomString();
            _jobTypes = new string[] { JobConstants.DefinitionNames.CreateInstructAllocationJob };

            _calculationEngineRunningChecker = new CalculationEngineRunningChecker(_jobs);
        }

        [TestMethod]
        public async Task CalculationEngineRunningChecker_WhenCreateAllocationJobsForSpecificationAreInProgress_TrueIsReturned()
        {
            //Arrange
            GivenTheJobForTheJobId(_ => {
                _.WithRunningStatus(RunningStatus.InProgress);
                _.WithJobType(_jobTypes.Single());
            });

            //Act
            bool running = await WhenTheCalculationEngineRunningStatusIsChecked();

            //Assert
            running
                .Should()
                .Be(true);
        }

        [TestMethod]
        public async Task CalculationEngineRunningChecker_WhenCreateAllocationJobsForSpecificationAreNotInProgress_FalseIsReturned()
        {
            //Arrange
            GivenTheJobForTheJobId(_ => _.WithJobType(_jobTypes.Single()));

            //Act
            bool running = await WhenTheCalculationEngineRunningStatusIsChecked();

            //Assert
            running
                .Should()
                .Be(false);
        }

        [TestMethod]
        public async Task CalculationEngineRunningChecker_WhenGetLatestJobsForSpecificationThrowsJobsNotRetrievedException_NonRetriableExceptionThrown()
        {
            //Arrange
            GivenTheJobForTheJobIdThatThrowsJobsNotRetrievedException();

            //Act
            Func<Task> func = async () => await WhenTheCalculationEngineRunningStatusIsChecked();

            //Assert
            await func
                .Should()
                .ThrowAsync<NonRetriableException>();
        }

        [TestMethod]
        public async Task CalculationEngineRunningChecker_WhenNoCreateAllocationJobsForSpecificationAreFound_FalseIsReturned()
        {
            //Act
            bool running = await WhenTheCalculationEngineRunningStatusIsChecked();

            //Assert
            running
                .Should()
                .Be(false);
        }

        private static RandomString NewRandomString()
        {
            return new RandomString();
        }

        private void GivenTheJobForTheJobId(Action<JobSummaryBuilder> setUp = null)
        {
            JobSummaryBuilder jobSummaryBuilder = new JobSummaryBuilder();

            setUp?.Invoke(jobSummaryBuilder);

            _job = jobSummaryBuilder.Build();

            _jobs.GetLatestJobsForSpecification(_specificationId, Arg.Is<IEnumerable<string>>(_ => _.Single() == JobConstants.DefinitionNames.CreateInstructAllocationJob))
                .Returns(new Dictionary<string, JobSummary> { { JobConstants.DefinitionNames.CreateInstructAllocationJob, _job } });
        }

        private void GivenTheJobForTheJobIdThatThrowsJobsNotRetrievedException()
        {
            JobSummaryBuilder jobSummaryBuilder = new JobSummaryBuilder();

            _jobs
                .When(_ => _.GetLatestJobsForSpecification(_specificationId, Arg.Is<IEnumerable<string>>(_ => _.Single() == JobConstants.DefinitionNames.CreateInstructAllocationJob)))
                .Throw(_ => { throw new JobsNotRetrievedException(string.Empty, new[] { JobConstants.DefinitionNames.CreateInstructAllocationJob }, _specificationId); });
        }

        private async Task<bool> WhenTheCalculationEngineRunningStatusIsChecked()
        {
            return await _calculationEngineRunningChecker.IsCalculationEngineRunning(_specificationId, _jobTypes);
        }
    }
}
