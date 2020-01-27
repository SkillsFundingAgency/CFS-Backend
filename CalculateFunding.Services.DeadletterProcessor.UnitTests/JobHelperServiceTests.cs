using System;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Services.DeadletterProcessor;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Core.Services
{
    [TestClass]
    public class JobHelperServiceTests
    {
        [TestMethod]
        public async Task ProcessDeadLetteredMessage_GivenMessageButNoJobId_LogsAnErrorAndDoesNotUpdadeJobLog()
        {
            // Arrange
            Message message = new Message();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            ILogger logger = CreateLogger();

            IJobHelperService service = CreateJobHelperService(jobsApiClient, logger);

            // Act
            await service.ProcessDeadLetteredMessage(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is("Missing job id from dead lettered message"));

            await
                jobsApiClient
                    .DidNotReceive()
                    .AddJobLog(Arg.Any<string>(), Arg.Any<JobLogUpdateModel>());
        }

        [TestMethod]
        public async Task ProcessDeadLetteredMessage_GivenMessageButAddingLogCausesException_LogsAnError()
        {
            // Arrange
            const string jobId = "job-id-1";

            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                    .When(x => x.AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>()))
                    .Do(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            IJobHelperService service = CreateJobHelperService(jobsApiClient, logger);

            // Act
            await service.ProcessDeadLetteredMessage(message);

            // Assert
            logger
                 .Received(1)
                 .Error(Arg.Any<Exception>(), Arg.Is($"Failed to add a job log for job id '{jobId}'"));
        }

        [TestMethod]
        public async Task ProcessDeadLetteredMessage_GivenMessageAndLogIsUpdated_LogsInformation()
        {
            // Arrange
            const string jobId = "job-id-1";

            JobLog jobLog = new JobLog
            {
                Id = "job-log-id-1"
            };

            ApiResponse<JobLog> jobLogResponse = new ApiResponse<JobLog>(HttpStatusCode.OK, jobLog);

            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>())
                .Returns(jobLogResponse);

            ILogger logger = CreateLogger();

            IJobHelperService service = CreateJobHelperService(jobsApiClient, logger);

            // Act
            await service.ProcessDeadLetteredMessage(message);

            // Assert
            logger
                .Received(1)
                .Information(Arg.Is($"A new job log was added to inform of a dead lettered message with job log id '{jobLog.Id}' on job with id '{jobId}'"));
        }

        private IJobHelperService CreateJobHelperService(IJobsApiClient jobsApiClient, ILogger logger = null)
        {
            IJobHelperResiliencePolicies resiliencePolicies = Substitute.For<IJobHelperResiliencePolicies>();
            resiliencePolicies.JobsApiClient.Returns(Polly.Policy.NoOpAsync());

            return new JobHelperService(jobsApiClient ?? CreateJobsApiClient(), resiliencePolicies, logger ?? CreateLogger());
        }

        private IJobsApiClient CreateJobsApiClient()
        {
            return Substitute.For<IJobsApiClient>();
        }

        private ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
    }
}
