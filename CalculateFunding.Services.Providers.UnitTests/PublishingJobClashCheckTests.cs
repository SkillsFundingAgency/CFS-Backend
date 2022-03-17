using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class PublishingJobClashCheckTests
    {
        private Mock<IJobManagement> _jobs;

        private PublishingJobClashCheck _jobClashCheck;
        
        [TestInitialize]
        public void SetUp()
        {
            _jobs = new Mock<IJobManagement>();

            _jobClashCheck = new PublishingJobClashCheck(_jobs.Object);
        }

        [TestMethod]
        [DataRow((string)null)]
        [DataRow("")]
        [DataRow("  ")]
        public void GuardsAgainstNoSpecificationIdBeingSupplied(string specificationId)
        {
            Func<bool> invocation = () => WhenJobClashesAreCheckedForTheSpecification(specificationId)
                .GetAwaiter()
                .GetResult();

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public async Task IsTrueIfJobsForSpecificationIdAndPublishingJobTypesAreRunning()
        {
            string specificationId = NewRandomString();

            IDictionary<string, JobSummary> latestJobs = new Dictionary<string, JobSummary>
            {
                { "job1", NewJobSummary(_ => _.WithRunningStatus(RunningStatus.InProgress)) },
                { "job2", NewJobSummary(_ => _.WithRunningStatus(RunningStatus.Completing)) },
            };

            GivenThePublishingJobSummariesForTheSpecificationId(specificationId, latestJobs);

            bool specificationHasClash = await WhenJobClashesAreCheckedForTheSpecification(specificationId);

            specificationHasClash
                .Should()
                .BeTrue();
        }
        
        [TestMethod]
        public async Task IsFalseIfJobsForSpecificationIdAndPublishingJobTypesAreRunning()
        {
            string specificationId = NewRandomString();

            IDictionary<string, JobSummary> latestJobs = new Dictionary<string, JobSummary>
            {
                { "job1", NewJobSummary(_ => _.WithRunningStatus(RunningStatus.Completed)) },
            };

            GivenThePublishingJobSummariesForTheSpecificationId(specificationId, latestJobs);

            bool specificationHasClash = await WhenJobClashesAreCheckedForTheSpecification(specificationId);

            specificationHasClash
                .Should()
                .BeFalse();  
        }

        [TestMethod]
        public async Task ThrowsExceptionIfGetLatestJobsForSpecificationThrowsException()
        {
            string specificationId = NewRandomString();

            GivenThePublishingJobSummariesForTheSpecificationIdThrowsException(specificationId);

            Func<Task> func = async () => await WhenJobClashesAreCheckedForTheSpecification(specificationId);

            await func
                .Should()
                .ThrowAsync<NonRetriableException>();
        }

        [TestMethod]
        public async Task IsFalseIfNoJobsForSpecificationIdAndPublishingJobTypesAtAll()
        {
            bool specificationHasClash = await WhenJobClashesAreCheckedForTheSpecification(NewRandomString());

            specificationHasClash
                .Should()
                .BeFalse();       
        }

        private JobSummary NewJobSummary(Action<JobSummaryBuilder> setUp = null)
        {
            JobSummaryBuilder jobSummaryBuilder = new JobSummaryBuilder();

            setUp?.Invoke(jobSummaryBuilder);
            
            return jobSummaryBuilder.Build();
        }

        private void GivenThePublishingJobSummariesForTheSpecificationId(string specificationId,
            IDictionary<string, JobSummary> jobSummaries)
            => _jobs.Setup(_ => _.GetLatestJobsForSpecification(It.Is<string>(id => id == specificationId),
                    It.Is<IEnumerable<string>>(types => types.SequenceEqual(new[]
                    {
                        JobConstants.DefinitionNames.RefreshFundingJob,
                        JobConstants.DefinitionNames.ApproveAllProviderFundingJob,
                        JobConstants.DefinitionNames.ApproveBatchProviderFundingJob,
                        JobConstants.DefinitionNames.PublishAllProviderFundingJob,
                        JobConstants.DefinitionNames.PublishBatchProviderFundingJob,
                        JobConstants.DefinitionNames.ReleaseProvidersToChannelsJob,
                    }))))
                .ReturnsAsync(jobSummaries);

        private void GivenThePublishingJobSummariesForTheSpecificationIdThrowsException(string specificationId)
            => _jobs
                .Setup(_ => _.GetLatestJobsForSpecification(It.Is<string>(id => id == specificationId),
                    It.Is<IEnumerable<string>>(types => types.SequenceEqual(new[]
                    {
                                JobConstants.DefinitionNames.RefreshFundingJob,
                                JobConstants.DefinitionNames.ApproveAllProviderFundingJob,
                                JobConstants.DefinitionNames.ApproveBatchProviderFundingJob,
                                JobConstants.DefinitionNames.PublishAllProviderFundingJob,
                                JobConstants.DefinitionNames.PublishBatchProviderFundingJob,
                                JobConstants.DefinitionNames.ReleaseProvidersToChannelsJob,
                    }))))
                .ThrowsAsync(new JobsNotRetrievedException(string.Empty, new[] { JobConstants.DefinitionNames.RefreshFundingJob }, specificationId));

        private string NewRandomString() => new RandomString();
        
        private async Task<bool> WhenJobClashesAreCheckedForTheSpecification(string specificationId)
            => await _jobClashCheck.PublishingJobsClashWithFundingStreamCoreProviderUpdate(specificationId);
    }
}