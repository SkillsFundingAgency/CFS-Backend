using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CalculateFunding.Functions.Jobs.ServiceBus
{
    public static class OnCheckForJobTimeout
    {
        /// <summary>
        /// Run every 30 minutes to query active jobs list and set any jobs which have exceeded execution time to timed out
        /// This is the catch all for when items don't get dead lettered or the job service unable to write status to cosmos
        /// </summary>
        /// <param name="myTimer"></param>
        /// <param name="log"></param>
        [FunctionName("check-job-timeout")]
        public static async Task Run([TimerTrigger("0 */30 * * * *")]TimerInfo timer)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            using (IServiceScope scope = IocConfig.Build(config).CreateScope())
            {
                IJobManagementService jobManagementService = scope.ServiceProvider.GetService<IJobManagementService>();
                ILogger logger = scope.ServiceProvider.GetService<ILogger>();

                try
                {
                    await jobManagementService.CheckAndProcessTimedOutJobs();
                }
                catch (Exception exception)
                {
                    logger.Error(exception, "An error occurred executing timer trigger 'check-job-timeout'");
                    throw;
                }

            }
        }
    }
}
