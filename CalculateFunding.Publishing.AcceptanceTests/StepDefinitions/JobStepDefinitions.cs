using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Services.Core.Constants;
using FluentAssertions;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class JobStepDefinitions
    {
        private readonly IJobStepContext _jobStepContext;
        private readonly ICurrentJobStepContext _currentJobStepContext;
        private readonly ICurrentSpecificationStepContext _currentSpecificationStepContext;

        public JobStepDefinitions(
            IJobStepContext jobStepContext,
            ICurrentJobStepContext currentJobStepContext,
            ICurrentSpecificationStepContext currentSpecificationStepContext)
        {
            _jobStepContext = jobStepContext;
            _currentJobStepContext = currentJobStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
        }

        [Given(@"the following job is requested to be queued for the current specification")]
        public void GivenTheFollowingJobIsRequestedToBeQueuedForTheCurrentSpecification(Table table)
        {
            JobCreateModel job = table.CreateInstance<JobCreateModel>();

            job.SpecificationId = _currentSpecificationStepContext.SpecificationId;

            _jobStepContext.JobToCreate = job;
        }

        [Given(@"the job is submitted to the job service")]
        public async Task GivenTheJobIsSubmittedToTheJobService()
        {
            Job job = await _jobStepContext.JobsClient.CreateJob(_jobStepContext.JobToCreate);
            _currentJobStepContext.JobId = job.Id;
        }

        [Then(@"the following job is requested is completed for the current specification")]
        public async Task ThenTheFollowingJobIsRequestedIsCompletedForTheCurrentSpecificationAsync(Table table)
        {
            JobCreateModel job = table.CreateInstance<JobCreateModel>();
            string jobId = _currentJobStepContext.JobId;
            List<JobLog> status = await _jobStepContext.InMemoryRepo.GetLatestJobForSpecification(jobId);

            var completeStatusLog = status.SingleOrDefault(c => c.CompletedSuccessfully == true);

            completeStatusLog.Should().NotBeNull();
        }

    }
}
