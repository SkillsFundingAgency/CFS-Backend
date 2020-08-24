using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public abstract class BasePrerequisiteChecker
    {
        private readonly IJobsRunning _jobsRunning;
        private readonly IJobManagement _jobManagement;
        private readonly ILogger _logger;
        
        public BasePrerequisiteChecker(IJobsRunning jobsRunning, IJobManagement jobManagement, ILogger logger)
        {
            Guard.ArgumentNotNull(jobsRunning, nameof(jobsRunning));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _jobsRunning = jobsRunning;
            _jobManagement = jobManagement;
            _logger = logger;
        }

        protected abstract Task<IEnumerable<string>> PerformChecks<T>(T prereqObject, IEnumerable<PublishedProvider> publishedProviders = null, IEnumerable<Provider> providers = null);

        public abstract bool IsCheckerType(PrerequisiteCheckerType type);

        protected async Task BasePerformChecks<T>(T prereqObject, string specificationId, string jobId, string[] jobDefinitions, IEnumerable<PublishedProvider> publishedProviders = null, IEnumerable<Provider> providers = null)
        {
            IEnumerable<string> jobTypesRunning = await _jobsRunning.GetJobTypes(specificationId, jobDefinitions);
            List<string> results = new List<string>();

            if (!jobTypesRunning.IsNullOrEmpty())
            {
                results.AddRange(jobTypesRunning.Select(_ => $"{_} is still running"));
                _logger.Error(string.Join(Environment.NewLine, results));
            }

            results.AddRange(await PerformChecks(prereqObject, publishedProviders, providers) ?? new string[0]);

            if (!results.IsNullOrEmpty())
            {
                if (!string.IsNullOrEmpty(jobId))
                {
                    await _jobManagement.UpdateJobStatus(jobId, completedSuccessfully: false, outcome: string.Join(", ", results));
                }

                string errorMessage = $"Specification with id: '{specificationId} has prerequisites which aren't complete.";
                throw new NonRetriableException(errorMessage);
            }
        }
    }
}
