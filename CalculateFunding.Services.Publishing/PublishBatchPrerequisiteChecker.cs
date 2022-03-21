﻿using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class PublishBatchPrerequisiteChecker : PublishAllPrerequisiteChecker, IPrerequisiteChecker
    {
        public override string Name => "Publish Batch Providers";

        public PublishBatchPrerequisiteChecker(
            ISpecificationFundingStatusService specificationFundingStatusService,
            IJobsRunning jobsRunning,
            IJobManagement jobManagement,
            ILogger logger) : base(specificationFundingStatusService, jobsRunning, jobManagement, logger)
        {
        }

        public override async Task PerformChecks<TSpecification>(TSpecification prereqObject, string jobId, IEnumerable<PublishedProvider> publishedProviders = null, IEnumerable<Provider> providers = null)
        {
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));
            SpecificationSummary specification = prereqObject as SpecificationSummary;
            Guard.ArgumentNotNull(specification, nameof(specification));

            await BasePerformChecks(prereqObject, specification.Id, jobId, new string[]
            {
                JobConstants.DefinitionNames.PublishedFundingUndoJob,
                JobConstants.DefinitionNames.RefreshFundingJob,
                JobConstants.DefinitionNames.ApproveAllProviderFundingJob,
                JobConstants.DefinitionNames.ApproveBatchProviderFundingJob,
                JobConstants.DefinitionNames.ReIndexPublishedProvidersJob,
                JobConstants.DefinitionNames.PublishAllProviderFundingJob,
                // don't include JobConstants.DefinitionNames.ReleaseProvidersToChannelsJob as the existing job is called within release to channels
            }, publishedProviders);
        }

        public override bool IsCheckerType(PrerequisiteCheckerType type)
        {
            return type == PrerequisiteCheckerType.ReleaseBatchProviders;
        }
    }
}
