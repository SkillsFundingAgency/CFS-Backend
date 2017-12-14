using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.Scenarios
{
    public static class GetEdubaseCsv
    {
        [FunctionName("GetEdubaseCsv")]
        public static void Run([TimerTrigger("0 0 2 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
