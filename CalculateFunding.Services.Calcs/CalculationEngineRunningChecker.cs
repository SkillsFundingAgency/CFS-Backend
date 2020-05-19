using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Helpers;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationEngineRunningChecker : ICalculationEngineRunningChecker
    {
        private readonly IJobManagement _jobManagement;

        public CalculationEngineRunningChecker(
            IJobManagement jobManagement)
        {
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));

            _jobManagement = jobManagement;
        }

        public async Task<bool> IsCalculationEngineRunning(string specificationId, IEnumerable<string> jobTypes)
        {
            Guard.ArgumentNotNull(jobTypes, nameof(jobTypes));

            IEnumerable<Task<JobSummary>> jobResponses = jobTypes
                .Select(async _ => await _jobManagement.GetLatestJobForSpecification(specificationId, new string[] { _ }));

            await TaskHelper.WhenAllAndThrow(jobResponses.ToArraySafe());

            return jobResponses.Any(_ => _.Result?.RunningStatus == RunningStatus.InProgress);
        }
    }
}
