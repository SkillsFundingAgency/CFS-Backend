using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Providers.UnitTests
{
    public class JobSummaryBuilder : TestEntityBuilder
    {
        private RunningStatus? _runningStatus;

        public JobSummaryBuilder WithRunningStatus(RunningStatus runningStatus)
        {
            _runningStatus = runningStatus;

            return this;
        }
        
        public JobSummary Build()
        {
            return new JobSummary
            {
                RunningStatus = _runningStatus.GetValueOrDefault(NewRandomEnum<RunningStatus>())
            };
        }
    }
}