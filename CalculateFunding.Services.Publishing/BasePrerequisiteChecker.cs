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
        private readonly ICalculationEngineRunningChecker _calculationEngineRunningChecker;
        private readonly IJobManagement _jobManagement;
        private readonly ILogger _logger;
        
        public BasePrerequisiteChecker(ICalculationEngineRunningChecker calculationEngineRunningChecker, IJobManagement jobManagement, ILogger logger)
        {
            Guard.ArgumentNotNull(calculationEngineRunningChecker, nameof(calculationEngineRunningChecker));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _calculationEngineRunningChecker = calculationEngineRunningChecker;
            _jobManagement = jobManagement;
            _logger = logger;
        }

        protected abstract Task<IEnumerable<string>> PerformChecks<T>(T prereqObject, IEnumerable<PublishedProvider> publishedProviders = null);

        public abstract bool IsCheckerType(PrerequisiteCheckerType type);

        protected async Task BasePerformChecks<T>(T prereqObject, string specificationId, string jobId, string[] jobDefinitions, IEnumerable<PublishedProvider> publishedProviders = null)
        {
            bool calculationEngineRunning = await _calculationEngineRunningChecker.IsCalculationEngineRunning(specificationId, jobDefinitions);
            List<string> results = new List<string>();

            if (calculationEngineRunning)
            {
                results.Add("Calculation engine is still running");
                _logger.Error(string.Join(Environment.NewLine, results));
            }

            results.AddRange(await PerformChecks(prereqObject, publishedProviders) ?? new string[0]);

            if (!results.IsNullOrEmpty())
            {
                string errorMessage = $"Specification with id: '{specificationId} has prerequisites which aren't complete.";

                await _jobManagement.UpdateJobStatus(jobId, completedSuccessfully: false, outcome: string.Join(", ", results));

                throw new NonRetriableException(errorMessage);
            }
        }
    }
}
