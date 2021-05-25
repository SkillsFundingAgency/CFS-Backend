using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Config.ApiClient.Jobs;
using CalculateFunding.IntegrationTests.Common.IoC;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.IntegrationTests.Common
{
    public abstract class IntegrationTestWithJobMonitoring : IntegrationTest
    {
        private IJobsApiClient _jobs;

        [TestInitialize]
        public void IntegrationTestWithJobMonitoringSetUp()
        {
            _jobs = GetService<IJobsApiClient>();
        }

        protected new static void SetUpServices(params Action<IServiceCollection, IConfiguration>[] setUps)
        {
            setUps = setUps.Concat(new Action<IServiceCollection, IConfiguration>[] {AddJobsClient})
                .ToArray();

            ServiceLocator = ServiceLocator.Create(Configuration, setUps);
        }

        protected async Task ThenTheJobSucceeds(string jobId,
            string failureMessage,
            int timeoutSeconds = 120)
            => await Wait.Until(() => TheJobSucceeds(jobId,
                    failureMessage),
                failureMessage,
                timeoutSeconds);

        protected async Task AndTheJobAndAllItsChildJobsSucceed(string jobId,
            string failureMessage,
            int timeoutSeconds = 120,
            string jobDefinitionId = null)
            => await ThenTheJobAndAllItsChildJobsSucceed(jobId, failureMessage, timeoutSeconds);

        protected async Task ThenTheJobAndAllItsChildJobsSucceed(string jobId,
            string failureMessage,
            int timeoutSeconds = 120,
            string jobDefinitionId = null)
            => await Wait.Until(() => TheJobAndAllChildrenSucceed(jobId,
                    failureMessage),
                failureMessage,
                timeoutSeconds);

        protected async Task<string[]> GetChildJobIds(string parentJobId,
            string jobDefinitionId = null)
        {
            JobViewModel job = await GetJob(parentJobId);

            return job?.ChildJobs?
                .Where(_ => jobDefinitionId == null || _.JobDefinitionId == jobDefinitionId)
                .Select(_ => _.Id)
                .ToArray();
        }

        protected async Task AndTheJobHasNoChildJobs(string jobId,
            string failureMessage)
        {
            JobViewModel job = await GetJob(jobId);

            job
                .Should()
                .NotBeNull();

            job
                .ChildJobs
                .Should()
                .BeNullOrEmpty(failureMessage);
        }

        protected async Task<bool> TheJobSucceeds(string jobId,
            string message)
        {
            ApiResponse<JobViewModel> jobResponse = await _jobs.GetJobById(jobId);

            JobViewModel job = jobResponse?.Content;

            if (job?.CompletionStatus == CompletionStatus.Failed)
            {
                throw new AssertFailedException(message);
            }

            return job?.RunningStatus == RunningStatus.Completed &&
                   job.CompletionStatus == CompletionStatus.Succeeded;
        }

        protected async Task<bool> TheJobAndAllChildrenSucceed(string jobId,
            string message,
            string jobDefinitionId = null)
        {
            ApiResponse<JobViewModel> jobResponse = await _jobs.GetJobById(jobId);

            JobViewModel job = jobResponse?.Content;

            bool parentSucceeded = job?.RunningStatus == RunningStatus.Completed &&
                                   job.CompletionStatus == CompletionStatus.Succeeded;

            if (TheJobFailed(job) || AnyChildJobsFailed(jobDefinitionId, job))
            {
                throw new AssertFailedException(message);
            }

            bool allChildrenSucceeded = TheAreAnyChildJobs(jobDefinitionId, job) &&
                                        AllTheChildJobsSucceeded(job);

            return parentSucceeded && allChildrenSucceeded;
        }

        private static bool AllTheChildJobsSucceeded(JobViewModel job) =>
            job.ChildJobs.All(_ => _.RunningStatus == RunningStatus.Completed &&
                                   _.CompletionStatus == CompletionStatus.Succeeded);

        private static bool TheAreAnyChildJobs(string jobDefinitionId,
            JobViewModel job) =>
            job.ChildJobs?.Where(_ => jobDefinitionId == null || _.JobDefinitionId == jobDefinitionId).Any() == true;

        private static bool TheJobFailed(JobViewModel job) => job?.CompletionStatus == CompletionStatus.Failed;

        private static bool AnyChildJobsFailed(string jobDefinitionId,
            JobViewModel job)
        {
            return job?.ChildJobs?.Where(_ => jobDefinitionId == null || _.JobDefinitionId == jobDefinitionId)
                .Any(_ => _.CompletionStatus == CompletionStatus.Failed) == true;
        }

        protected async Task<JobViewModel> GetJob(string jobId)
            => (await _jobs.GetJobById(jobId))?.Content;

        protected static void AddJobsClient(IServiceCollection serviceCollection,
            IConfiguration configuration)
            => serviceCollection.AddJobsInterServiceClient(configuration);
    }
}