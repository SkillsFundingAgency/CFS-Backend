using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.Jobs.ServiceBus
{
    public static class CheckForJobTimeout
    {
        /// <summary>
        /// Run every 30 minutes to query active jobs list and set any jobs which have exceeded execution time to timed out
        /// This is the catch all for when items don't get dead lettered or the job service unable to write status to cosmos
        /// </summary>
        /// <param name="myTimer"></param>
        /// <param name="log"></param>
        [FunctionName("check-job-timeout")]
        public static void Run([TimerTrigger("0 */30 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
