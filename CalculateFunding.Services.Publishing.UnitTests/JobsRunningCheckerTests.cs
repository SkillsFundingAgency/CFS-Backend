using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class JobsRunningCheckerTests

    {
        private IJobsApiClient _jobs;
        private JobSummary _job;
        private string _specificationId;
        private string[] _jobTypes;

        private JobsRunning _jobsRunning;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = Substitute.For<IJobsApiClient>();
            _specificationId = NewRandomString();
            _jobTypes = new string[] { JobConstants.DefinitionNames.CreateInstructAllocationJob };

            _jobsRunning = new JobsRunning(_jobs,
                new ResiliencePolicies
                {
                    JobsApiClient = Policy.NoOpAsync()
                });
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
            IEnumerable<string> jobTypes = await WhenJobsAreRunningStatusIsChecked();

            //Assert
            jobTypes
                .Should()
                .NotBeEmpty();
        }

        [TestMethod]
        public async Task CalculationEngineRunningChecker_WhenCreateAllocationJobsForSpecificationAreNotInProgress_FalseIsReturned()
        {
            //Arrange
            GivenTheJobForTheJobId(_ => _.WithJobType(_jobTypes.Single()));

            //Act
            IEnumerable<string> jobTypes = await WhenJobsAreRunningStatusIsChecked();

            //Assert
            jobTypes
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task CalculationEngineRunningChecker_WhenNoCreateAllocationJobsForSpecificationAreFound_FalseIsReturned()
        {
            //Act
            IEnumerable<string> jobTypes = await WhenJobsAreRunningStatusIsChecked();

            //Assert
            jobTypes
                .Should()
                .BeEmpty();
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

            _jobs.GetLatestJobForSpecification(_specificationId, Arg.Is<IEnumerable<string>>(_ => _.Single() == JobConstants.DefinitionNames.CreateInstructAllocationJob))
                .Returns(new ApiResponse<JobSummary>(HttpStatusCode.OK, _job));
        }

        private async Task<IEnumerable<string>> WhenJobsAreRunningStatusIsChecked()
        {
            return await _jobsRunning.GetJobTypes(_specificationId, _jobTypes);
        }
    }
}
