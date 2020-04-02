using System;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public class ApplyTemplateCalculationsJobTrackerFactory : IApplyTemplateCalculationsJobTrackerFactory
    {
        private readonly ILogger _logger;
        private readonly AsyncPolicy _jobsResiliencePolicy;
        private readonly IJobsApiClient _jobs;

        public ApplyTemplateCalculationsJobTrackerFactory(IJobsApiClient jobs,
            ICalcsResiliencePolicies calculationsResiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(calculationsResiliencePolicies?.JobsApiClient, nameof(calculationsResiliencePolicies.JobsApiClient));

            _logger = logger;
            _jobs = jobs;
            _jobsResiliencePolicy = calculationsResiliencePolicies.JobsApiClient;
        }

        public IApplyTemplateCalculationsJobTracker CreateJobTracker(Message message)
        {
            string jobId = message.GetUserProperty<string>("jobId");

            if (jobId.IsNullOrWhitespace())
            {
                const string error = "No JobId property in message";

                _logger.Error(error);

                throw new Exception(error);
            }

            return new ApplyTemplateCalculationsJobTracker(jobId,
                _jobs,
                _jobsResiliencePolicy,
                _logger);
        }
    }
}