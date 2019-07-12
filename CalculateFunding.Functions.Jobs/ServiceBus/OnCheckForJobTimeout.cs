using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Jobs.ServiceBus
{
    public class OnCheckForJobTimeout
    {
        private readonly ILogger _logger;
        private readonly IJobManagementService _jobManagementService;

        public OnCheckForJobTimeout(
            ILogger logger,
            IJobManagementService jobManagementService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobManagementService, nameof(jobManagementService));

            _logger = logger;
            _jobManagementService = jobManagementService;
        }

        /// <summary>
        /// Run every 30 minutes to query active jobs list and set any jobs which have exceeded execution time to timed out
        /// This is the catch all for when items don't get dead lettered or the job service unable to write status to cosmos
        /// </summary>
        /// <param name="myTimer"></param>
        /// <param name="log"></param>
        [FunctionName("check-job-timeout")]
        public async Task Run([TimerTrigger("0 */30 * * * *")]TimerInfo timer)
        {
            try
            {
                await _jobManagementService.CheckAndProcessTimedOutJobs();
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "An error occurred executing timer trigger 'check-job-timeout'");
                throw;
            }
        }
    }
}
