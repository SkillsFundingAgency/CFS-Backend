using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.Services
{
    public class JobViewModelBuilder : TestEntityBuilder
    {
        private CompletionStatus? _completionStatus;

        public JobViewModelBuilder WithCompletionStatus(CompletionStatus completionStatus)
        {
            _completionStatus = completionStatus;

            return this;
        }

        public JobViewModel Build()
        {
            return new JobViewModel
            {
                CompletionStatus = _completionStatus
            };
        }
    }
}