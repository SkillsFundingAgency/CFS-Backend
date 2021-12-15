using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Tests.Common.Helpers;
using System;

namespace CalculateFunding.Services.Results.UnitTests
{
    public class JobSummaryBuilder : TestEntityBuilder
    {
        private RunningStatus _inProgressStatus;
        private string _jobType;
        private string _jobId;
        private DateTimeOffset _lastUpdated;
        private string _invokerUserDisplayName;

        public JobSummaryBuilder WithRunningStatus(RunningStatus inProgressStatus)
        {
            _inProgressStatus = inProgressStatus;

            return this;
        }

        public JobSummaryBuilder WithJobType(string jobType)
        {
            _jobType = jobType;

            return this;
        }

        public JobSummaryBuilder WithJobId(string jobId)
        {
            _jobId = jobId;

            return this;
        }

        public JobSummaryBuilder WithLastUpdated(DateTimeOffset lastUpdated)
        {
            _lastUpdated = lastUpdated;

            return this;
        }

        public JobSummaryBuilder WithInvokerUserDisplayName(string invokerUserDisplayName)
        {
            _invokerUserDisplayName = invokerUserDisplayName;

            return this;
        }

        public JobSummary Build()
        {
            return new JobSummary
            {
                JobId = _jobId ?? NewRandomString(),
                RunningStatus = _inProgressStatus,
                JobType = _jobType,
                LastUpdated = _lastUpdated,
                InvokerUserDisplayName = _invokerUserDisplayName
            };
        }
    }
}
