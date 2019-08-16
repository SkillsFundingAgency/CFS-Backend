using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class CalculationEngineRunningChecker : ICalculationEngineRunningChecker
    {
        public CalculationEngineRunningChecker(
            IJobsApiClient jobsApiClient,
            IPublishingResiliencePolicies publishingResiliencePolicies)
        {

        }

        public Task<bool> IsCalculationEngineRunning(string specificationId)
        {
            // Use the jobs service to get the latest instruct calculation job for this specification and make sure the running status isn't still running
            throw new System.NotImplementedException();
        }
    }
}
