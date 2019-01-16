using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.FeatureToggles;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Services
{
    public partial class PublishedResultsServiceTests
    {
        [TestMethod]
        public async Task UpdateDeadLetteredJobLog_GivenMessageButNoJobId_LogsAnErrorAndDoesNotUpdadeJobLog()
        {
            //Arrange
            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
               .IsApprovalBatchingServerSideEnabled()
               .Returns(true);

            ILogger logger = CreateLogger();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            Message message = new Message();

            PublishedResultsService service = CreateResultsService(logger, featureToggle: featureToggle, jobsApiClient: jobsApiClient);

            //Act
            await service.UpdateDeadLetteredJobLog(message);

            //Assert
           logger
                .Received(1)
                .Error(Arg.Is("Missing job id from dead lettered message"));

            await
                jobsApiClient
                    .DidNotReceive()
                    .AddJobLog(Arg.Any<string>(), Arg.Any<JobLogUpdateModel>());
        }

        [TestMethod]
        public async Task UpdateDeadLetteredJobLog_GivenMessageButAddingLogCausesException_LogsAnError()
        {
            //Arrange
            const string jobId = "job-id-1";

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
               .IsApprovalBatchingServerSideEnabled()
               .Returns(true);

            ILogger logger = CreateLogger();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            jobsApiClient
               .When(x => x.AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>()))
               .Do(x => { throw new Exception(); });

            PublishedResultsService service = CreateResultsService(logger, featureToggle: featureToggle, jobsApiClient: jobsApiClient);

            //Act
            await service.UpdateDeadLetteredJobLog(message);

            //Assert
            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Failed to add a job log for job id '{jobId}'"));
        }

        [TestMethod]
        public async Task UpdateDeadLetteredJobLog_GivenMessageAndLogIsUpdated_LogsInformation()
        {
            //Arrange
            const string jobId = "job-id-1";

            JobLog jobLog = new JobLog
            {
                Id = "job-log-id-1"
            };

            ApiResponse<JobLog> jobLogResponse = new ApiResponse<JobLog>(HttpStatusCode.OK, jobLog);

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
               .IsApprovalBatchingServerSideEnabled()
               .Returns(true);

            ILogger logger = CreateLogger();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            jobsApiClient
                    .AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>())
                    .Returns(jobLogResponse);

            PublishedResultsService service = CreateResultsService(logger, featureToggle: featureToggle, jobsApiClient: jobsApiClient);

            //Act
            await service.UpdateDeadLetteredJobLog(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"A new job log was added to inform of a dead lettered message with job log id '{jobLog.Id}' on job with id '{jobId}' while attempting to generate allocations"));
        }

        [TestMethod]
        public async Task UpdateDeadLetteredJobLog_GivenMessageAndFeatureToggleIsTurnedOff_DoesNotAddJobLog()
        {
            //Arrange
            JobLog jobLog = new JobLog
            {
                Id = "job-log-id-1"
            };

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
               .IsApprovalBatchingServerSideEnabled()
               .Returns(false);

            Message message = new Message();

            PublishedResultsService service = CreateResultsService(featureToggle: featureToggle, jobsApiClient: jobsApiClient);

            //Act
            await service.UpdateDeadLetteredJobLog(message);

            //Assert
            await
                jobsApiClient
                    .DidNotReceive()
                    .AddJobLog(Arg.Any<string>(), Arg.Any<JobLogUpdateModel>());
        }
    }
}
