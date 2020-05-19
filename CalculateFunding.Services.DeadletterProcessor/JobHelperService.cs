using System;
using System.Threading.Tasks;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.DeadletterProcessor
{
    public class JobHelperService : IJobHelperService
    {
        private readonly IJobManagement _jobManagement;
        private readonly ILogger _logger;

        public JobHelperService(IJobManagement jobManagement, ILogger logger)
        {
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _jobManagement = jobManagement;
            _logger = logger;
        }

        public async Task ProcessDeadLetteredMessage(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            if (!message.UserProperties.ContainsKey("jobId"))
            {
                _logger.Error("Missing job id from dead lettered message");
                return;
            }

            string jobId = message.UserProperties["jobId"].ToString();

            Common.ApiClient.Jobs.Models.JobLogUpdateModel jobLogUpdateModel = new Common.ApiClient.Jobs.Models.JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = $"The job has exceeded its maximum retry count and failed to complete successfully"
            };

            try
            {
                Common.ApiClient.Jobs.Models.JobLog jobLog = await _jobManagement.AddJobLog(jobId, jobLogUpdateModel);

                if (jobLog == null)
                {
                    _logger.Error($"Failed to add a job log for job id '{jobId}'");
                }
                else
                {
                    _logger.Information($"A new job log was added to inform of a dead lettered message with job log id '{jobLog.Id}' on job with id '{jobId}'");
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Failed to add a job log for job id '{jobId}'");
            }
        }
    }
}
