using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class ApprovePrerequisiteChecker : IApprovePrerequisiteChecker
    {
        private readonly ICalculationEngineRunningChecker _calculationEngineRunningChecker;
        private readonly ILogger _logger;

        public ApprovePrerequisiteChecker(
            ICalculationEngineRunningChecker calculationEngineRunningChecker,
            ILogger logger)
        {
            Guard.ArgumentNotNull(calculationEngineRunningChecker, nameof(calculationEngineRunningChecker));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _calculationEngineRunningChecker = calculationEngineRunningChecker;
            _logger = logger;
        }

        public async Task<IEnumerable<string>> PerformPrerequisiteChecks(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            bool calculationEngineRunning = await _calculationEngineRunningChecker.IsCalculationEngineRunning(specificationId, new string[] { JobConstants.DefinitionNames.RefreshFundingJob, JobConstants.DefinitionNames.PublishProviderFundingJob });
            List<string> results = new List<string>();

            if (calculationEngineRunning)
            {
                results.Add("Calculation engine is still running");
                _logger.Error(string.Join(Environment.NewLine, results));
                return results;
            }

            return results;
        }
    }
}
