using System;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public class ApplyTemplateCalculationsJobTrackerFactory : IApplyTemplateCalculationsJobTrackerFactory
    {
        private readonly ILogger _logger;
        private readonly IJobManagement _jobs;

        public ApplyTemplateCalculationsJobTrackerFactory(IJobManagement jobs,
            ILogger logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobs, nameof(jobs));

            _logger = logger;
            _jobs = jobs;
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
                _logger);
        }
    }
}