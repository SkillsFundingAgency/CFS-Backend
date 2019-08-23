using System;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Services.Core;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class ApproveResultsJobTrackerTests
    {
        private IJobsApiClient _jobs;
        private JobViewModel _job;
        private string _jobId;

        private ApproveResultsJobTracker _tracker;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = Substitute.For<IJobsApiClient>();
            _jobId = NewRandomString();

            _tracker = new ApproveResultsJobTracker(_jobs,
                new ResiliencePolicies
                {
                    JobsApiClient = Policy.NoOpAsync()
                }, 
                Substitute.For<ILogger>());
        }

        [TestMethod]
        public void TryStartThrowsNonRetriableExceptionIfNoJobFoundWithJobId()
        {
            Func<Task<bool>> invocation = WhenTheJobTrackingIsStarted;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .WithMessage($"Could not find the job with job id: '{_jobId}'");
        }

        [TestMethod]
        public async Task TryStartLogsAnInitialJobUpdate()
        {
            GivenTheJobForTheJobId();

            bool trackingStarted = await WhenTheJobTrackingIsStarted();

            trackingStarted
                .Should()
                .BeTrue();

            AndTheJobWasAddedForTheJobId(_ => _.Outcome.IsNullOrWhitespace() &&
                                              _.CompletedSuccessfully == null &&
                                              _.ItemsFailed == 0 &&
                                              _.ItemsSucceeded == 0 &&
                                              _.ItemsProcessed == 0);
        }

        [TestMethod]
        public async Task TryStartExitsEarlyIfTheJobAlreadyHasAStatus()
        {
            GivenTheJobForTheJobId(_ => _.WithCompletionStatus(CompletionStatus.Superseded));

            bool trackingStarted = await WhenTheJobTrackingIsStarted();

            trackingStarted
                .Should()
                .BeFalse();

            AndTheJobWasNotAddedForTheJobId(_ => _.Outcome.IsNullOrWhitespace() &&
                                                 _.CompletedSuccessfully == null &&
                                                 _.ItemsFailed == 0 &&
                                                 _.ItemsSucceeded == 0 &&
                                                 _.ItemsProcessed == 0);
        }

        [TestMethod]
        public async Task CompleteTrackingSendsJobLogWithItemCountAndCompletedSuccessfullyFlag()
        {
            await _tracker.CompleteTrackingJob(_jobId);

            AndTheJobWasAddedForTheJobId(_ => _.Outcome == null &&
                                              _.CompletedSuccessfully == true &&
                                              _.ItemsFailed == 0 &&
                                              _.ItemsSucceeded == 0 &&
                                              _.ItemsProcessed == 0);
        }

        private static RandomNumberBetween NewRandomNumber()
        {
            return new RandomNumberBetween(1, int.MaxValue);
        }

        private static RandomString NewRandomString()
        {
            return new RandomString();
        }

        private async Task<bool> WhenTheJobTrackingIsStarted()
        {
            return await _tracker.TryStartTrackingJob(_jobId);
        }

        private void GivenTheJobForTheJobId(Action<JobViewModelBuilder> setUp = null)
        {
            JobViewModelBuilder jobViewModelBuilder = new JobViewModelBuilder();

            setUp?.Invoke(jobViewModelBuilder);

            _job = jobViewModelBuilder.Build();

            _jobs.GetJobById(_jobId)
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, _job));
        }

        private void AndTheJobWasNotAddedForTheJobId(Expression<Predicate<JobLogUpdateModel>> jobLogMatching)
        {
            AndTheJobWasAddedForTheJobId(jobLogMatching, 0);
        }

        private void AndTheJobWasAddedForTheJobId(Expression<Predicate<JobLogUpdateModel>> jobLogMatching, int times = 1)
        {
            _jobs
                .Received(times)
                .AddJobLog(_jobId, Arg.Is(jobLogMatching));
        }
    }
}