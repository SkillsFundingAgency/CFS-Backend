using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class SqlImportPreRequisiteChecker
        : BasePrerequisiteChecker, IPrerequisiteChecker
    {
        public override string Name => "Sql Import";

        public SqlImportPreRequisiteChecker(
            IJobsRunning jobsRunning,
            IJobManagement jobManagement,
            ILogger logger) : base(jobsRunning, jobManagement, logger)
        {
        }

        public virtual async Task PerformChecks<TSpecification>(TSpecification prereqObject, string jobId, IEnumerable<PublishedProvider> publishedProviders = null, IEnumerable<Provider> providers = null)
        {
            SpecificationSummary specification = prereqObject as SpecificationSummary;
            Guard.ArgumentNotNull(specification, nameof(specification));

            await BasePerformChecks(prereqObject, specification.Id, jobId, new string[]
            {
                JobConstants.DefinitionNames.PublishedFundingUndoJob
            }, publishedProviders);
        }

        protected async override Task<IEnumerable<string>> PerformChecks<TSpecification>(TSpecification prereqObject, IEnumerable<PublishedProvider> publishedProviders = null, IEnumerable<Provider> providers = null)
        {
            return await Task.FromResult(ArraySegment<string>.Empty);
        }
    
        public override bool IsCheckerType(PrerequisiteCheckerType type)
        {
            return type == PrerequisiteCheckerType.SqlImport;
        }
    }
}
