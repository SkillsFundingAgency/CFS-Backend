using System;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.JobManagement;
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

            IJobManagement jobManagement = CreateJobManagement();

            ILogger logger = CreateLogger();

            IJobHelperService service = CreateJobHelperService(jobManagement, logger);

            // Act
            await service.ProcessDeadLetteredMessage(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is("Missing job id from dead lettered message"));

            await
                jobManagement
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

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                    .When(x => x.AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>()))
                    .Do(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            IJobHelperService service = CreateJobHelperService(jobManagement, logger);

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

            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>())
                .Returns(jobLog);

            ILogger logger = CreateLogger();

            IJobHelperService service = CreateJobHelperService(jobManagement, logger);

            // Act
            await service.ProcessDeadLetteredMessage(message);

            // Assert
            logger
                .Received(1)
                .Information(Arg.Is($"A new job log was added to inform of a dead lettered message with job log id '{jobLog.Id}' on job with id '{jobId}'"));
        }

        private IJobHelperService CreateJobHelperService(IJobManagement jobManagement, ILogger logger = null)
        {
            return new JobHelperService(jobManagement ?? CreateJobManagement(), logger ?? CreateLogger());
        }

        private IJobManagement CreateJobManagement() => Substitute.For<IJobManagement>();

        private ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
    }
}
