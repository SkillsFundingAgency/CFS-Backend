using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class ApprovePrerequisiteChecker : BasePrerequisiteChecker, IPrerequisiteChecker
    {
        public ApprovePrerequisiteChecker(
            ICalculationEngineRunningChecker calculationEngineRunningChecker,
            IJobManagement jobManagement,
            ILogger logger) : base(calculationEngineRunningChecker, jobManagement, logger)
        {
        }

        public async Task PerformChecks<T>(T prereqObject, string jobId, IEnumerable<PublishedProvider> publishedProviders = null)
        {
            string specificationId = prereqObject as string;

            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            await BasePerformChecks(specificationId, specificationId, jobId, new string[] { JobConstants.DefinitionNames.RefreshFundingJob, JobConstants.DefinitionNames.PublishProviderFundingJob, JobConstants.DefinitionNames.ReIndexPublishedProvidersJob });
        }

        protected override Task<IEnumerable<string>> PerformChecks<T>(T prereqObject, IEnumerable<PublishedProvider> publishedProviders = null)
        {
            return Task.FromResult<IEnumerable<string>>(null);
        }

        public override bool IsCheckerType(PrerequisiteCheckerType type)
        {
            return type == PrerequisiteCheckerType.Approve;
        }
    }
}
