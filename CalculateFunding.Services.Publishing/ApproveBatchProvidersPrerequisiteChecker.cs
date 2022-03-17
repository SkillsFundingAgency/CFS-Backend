using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class ApproveBatchProvidersPrerequisiteChecker : BasePrerequisiteChecker, IPrerequisiteChecker
    {
        public override string Name => "Approve Batch Providers";

        public ApproveBatchProvidersPrerequisiteChecker(
            IJobsRunning jobsRunning,
            IJobManagement jobManagement,
            ILogger logger) : base(jobsRunning, jobManagement, logger)
        {
        }

        public async Task PerformChecks<T>(T prereqObject, string jobId, IEnumerable<PublishedProvider> publishedProviders = null, IEnumerable<Provider> providers = null)
        {
            string specificationId = prereqObject as string;

            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            await BasePerformChecks(
                specificationId, 
                specificationId, 
                jobId, 
                new string[]
                {
                    JobConstants.DefinitionNames.PublishedFundingUndoJob,
                    JobConstants.DefinitionNames.RefreshFundingJob, 
                    JobConstants.DefinitionNames.PublishAllProviderFundingJob,
                    JobConstants.DefinitionNames.PublishBatchProviderFundingJob,
                    JobConstants.DefinitionNames.ReleaseProvidersToChannelsJob,
                    JobConstants.DefinitionNames.ReIndexPublishedProvidersJob,
                    JobConstants.DefinitionNames.ApproveAllProviderFundingJob
                });
        }

        protected override Task<IEnumerable<string>> PerformChecks<T>(T prereqObject, IEnumerable<PublishedProvider> publishedProviders = null, IEnumerable<Provider> providers = null)
        {
            return Task.FromResult<IEnumerable<string>>(null);
        }

        public override bool IsCheckerType(PrerequisiteCheckerType type)
        {
            return type == PrerequisiteCheckerType.ApproveBatchProviders;
        }
    }
}
