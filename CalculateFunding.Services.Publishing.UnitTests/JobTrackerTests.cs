using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class JobTrackerTests
    {
        private IJobManagement _jobs;
        private JobViewModel _job;
        private string _jobId;

        private JobTracker _tracker;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = Substitute.For<IJobManagement>();
            _jobId = NewRandomString();

            _tracker = new JobTracker(_jobs);
        }

        [TestMethod]
        public void TryStartThrowsNonRetriableExceptionIfNoJobFoundWithJobId()
        {
            GivenTheJobNotFound();

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
            GivenTheJobAlreadyCompleted(_ => _.WithCompletionStatus(CompletionStatus.Superseded));

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
        
        [TestMethod]
        public async Task NotifyProgressAddJoLogWithSuppliedItemCount()
        {
            int expectedItemCount = NewRandomNumber();

            await _tracker.NotifyProgress(expectedItemCount, _jobId);

            AndTheJobWasAddedForTheJobId(_ => _.Outcome.IsNullOrWhitespace() &&
                                              _.CompletedSuccessfully == null &&
                                              _.ItemsFailed == null &&
                                              _.ItemsSucceeded == null &&
                                              _.ItemsProcessed == expectedItemCount);
        }

        [TestMethod]
        public async Task FailJobSendsJobLogWithSuppliedOutcomeAndFailedFlag()
        {
            string expectedOutcome = NewRandomString();

            await _tracker.FailJob(expectedOutcome, _jobId);

            AndTheJobWasAddedForTheJobId(_ => _.Outcome == expectedOutcome &&
                                              _.CompletedSuccessfully == false &&
                                              _.ItemsFailed == null &&
                                              _.ItemsSucceeded == null &&
                                              _.ItemsProcessed == null);
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
            return await _tracker.TryStartTrackingJob(_jobId, "ApproveJob");
        }

        private void GivenTheJobForTheJobId(Action<JobViewModelBuilder> setUp = null)
        {
            JobViewModelBuilder jobViewModelBuilder = new JobViewModelBuilder();

            setUp?.Invoke(jobViewModelBuilder);

            _job = jobViewModelBuilder.Build();

            _jobs.RetrieveJobAndCheckCanBeProcessed(_jobId)
                .Returns(_job);
        }

        private void GivenTheJobAlreadyCompleted(Action<JobViewModelBuilder> setUp = null)
        {
            JobViewModelBuilder jobViewModelBuilder = new JobViewModelBuilder();

            setUp?.Invoke(jobViewModelBuilder);

            _job = jobViewModelBuilder.Build();

            _jobs.RetrieveJobAndCheckCanBeProcessed(_jobId)
                .Throws(new JobAlreadyCompletedException(string.Empty, _job));
        }

        private void GivenTheJobNotFound()
        {
            _jobs.RetrieveJobAndCheckCanBeProcessed(_jobId)
                .Throws(new JobNotFoundException($"Could not find the job with job id: '{_jobId}'", _jobId));
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