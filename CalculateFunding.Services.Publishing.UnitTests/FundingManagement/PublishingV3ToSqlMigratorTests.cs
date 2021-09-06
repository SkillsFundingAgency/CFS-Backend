using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.FundingManagement;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.FundingManagement
{
    [TestClass]
    public class PublishingV3ToSqlMigratorTests
    {
        private const string MigrationKey = "migration-key";
        private const string MigrationKeyValue = "6695d9f9-079f-4afe-ac13-53ca1dd39e28";
        
        private string _correlationId;

        private IPublishingV3ToSqlMigrator _publishingV3ToSqlMigrator;
        private Mock<IReleaseManagementRepository> _releaseManagementRepository;
        private Mock<ISpecificationsApiClient> _specificationsApiClient;
        private Mock<IPoliciesApiClient> _policiesApiClient;
        private Mock<IPublishedFundingReleaseManagementMigrator> _publishedFundingReleaseManagementMigrator;
        private Mock<ILogger> _logger;
        private Mock<IJobManagement> _jobManagement;

        [TestInitialize]
        public void SetUp()
        {
            _correlationId = new RandomString();

            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();
            _specificationsApiClient = new Mock<ISpecificationsApiClient>();
            _policiesApiClient = new Mock<IPoliciesApiClient>();
            _logger = new Mock<ILogger>();
            _publishedFundingReleaseManagementMigrator = new Mock<IPublishedFundingReleaseManagementMigrator>();
            _jobManagement = new Mock<IJobManagement>();

            _publishingV3ToSqlMigrator = new PublishingV3ToSqlMigrator(
                _releaseManagementRepository.Object,
                _specificationsApiClient.Object,
                _policiesApiClient.Object,
                _logger.Object,
                _publishedFundingReleaseManagementMigrator.Object,
                _jobManagement.Object,
                PublishingResilienceTestHelper.GenerateTestPolicies()
            );
        }

        [TestMethod]
        public async Task QueueReleaseManagementDataMigrationJob_WhenPreRequisitesMet_JobQueued()
        {
            GivenJobQueued();
            await WhenQueueReleaseManagementDataMigrationJob();
        }

        [TestMethod]
        public void QueueReleaseManagementDataMigrationJob_WhenJobAlreadyRunning_ExceptionThrown()
        {
            string jobRunningId = new RandomString();
            GivenJobsRunning(jobRunningId);
            Func<Task> invocation = WhenQueueReleaseManagementDataMigrationJob;

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be($"Unable to queue a new release managment data migration job as one is already running job id:{jobRunningId}.");
        }

        private void GivenJobsRunning(string jobRunningId)
        {
            _jobManagement.Setup(_ => _.GetLatestJobs(It.Is<IEnumerable<string>>(jobTypes =>
                (
                    jobTypes.First() == JobConstants.DefinitionNames.ReleaseManagmentDataMigrationJob
                )
            ), null))
            .ReturnsAsync(
                NewJobSummary(_ => _.WithJobType(JobConstants.DefinitionNames.ReleaseManagmentDataMigrationJob)
                                    .WithRunningStatus(RunningStatus.InProgress)
                                    .WithJobId(jobRunningId)
            ).ToDictionary(_ => _.JobType));
        }

        private void GivenJobQueued()
        {
            _jobManagement.Setup(_ => _.QueueJob(
                It.Is<JobCreateModel>(job =>
                    job.CorrelationId == _correlationId &&
                    job.JobDefinitionId == JobConstants.DefinitionNames.ReleaseManagmentDataMigrationJob &&
                    job.Properties[MigrationKey] == MigrationKeyValue
                )
            ))
            .ReturnsAsync(new Job
            {
                CorrelationId = _correlationId,
                JobDefinitionId = JobConstants.DefinitionNames.ReleaseManagmentDataMigrationJob
            });
        }

        private async Task WhenQueueReleaseManagementDataMigrationJob()
        {
            await _publishingV3ToSqlMigrator.QueueReleaseManagementDataMigrationJob(
                NewReference(),
                _correlationId
            );
        }

        private static Reference NewReference(Action<ReferenceBuilder> setup = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();
            
            setup?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        private static IEnumerable<JobSummary> NewJobSummary(Action<JobSummaryBuilder> setup = null)
        {
            JobSummaryBuilder jobSummaryBuilder = new JobSummaryBuilder();

            setup?.Invoke(jobSummaryBuilder);

            return jobSummaryBuilder.Build();
        }
    }
}
