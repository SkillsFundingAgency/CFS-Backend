using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.Jobs
{
    public static class CheckForJobTimeout
    {
        /// <summary>
        /// Run every minute to query active jobs list and set any jobs which have exceeded execution time to timed out
        /// </summary>
        /// <param name="myTimer"></param>
        /// <param name="log"></param>
        [FunctionName("check-job-timeout")]
        public static void Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
