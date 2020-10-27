using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class JobViewModelBuilder : TestEntityBuilder
    {
        private CompletionStatus? _completionStatus;
        private string _jobId;

        public JobViewModelBuilder WithJobId(string jobId)
        {
            _jobId = jobId;

            return this;
        }

        public JobViewModelBuilder WithCompletionStatus(CompletionStatus completionStatus)
        {
            _completionStatus = completionStatus;

            return this;
        }

        public JobViewModel Build()
        {
            return new JobViewModel
            {
                Id = _jobId ?? new RandomString(),
                CompletionStatus = _completionStatus
            };
        }
    }
}