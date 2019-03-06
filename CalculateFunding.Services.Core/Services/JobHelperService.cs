using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Services;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.Core.Services
{
    public class JobHelperService : IJobHelperService
    {
        private readonly IJobsApiClient _jobsApiClient;
        private readonly Polly.Policy _jobsApiClientPolicy;
        private readonly ILogger _logger;

        public JobHelperService(IJobsApiClient jobsApiClient, IJobHelperResiliencePolicies resiliencePolicies, ILogger logger)
        {
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _jobsApiClient = jobsApiClient;
            _jobsApiClientPolicy = resiliencePolicies.JobsApiClient;
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
                ApiResponse<Common.ApiClient.Jobs.Models.JobLog> jobLogResponse = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(jobId, jobLogUpdateModel));

                if (jobLogResponse == null || jobLogResponse.Content == null)
                {
                    _logger.Error($"Failed to add a job log for job id '{jobId}'");
                }
                else
                {
                    _logger.Information($"A new job log was added to inform of a dead lettered message with job log id '{jobLogResponse.Content.Id}' on job with id '{jobId}'");
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Failed to add a job log for job id '{jobId}'");
            }
        }
    }
}
