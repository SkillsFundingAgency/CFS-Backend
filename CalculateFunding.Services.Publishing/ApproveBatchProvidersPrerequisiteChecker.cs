﻿using System.Collections.Generic;
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
        public ApproveBatchProvidersPrerequisiteChecker(
            IJobsRunning jobsRunning,
            IJobManagement jobManagement,
            ILogger logger) : base(jobsRunning, jobManagement, logger)
        {
        }

        public async Task PerformChecks<T>(T prereqObject, string jobId, IEnumerable<PublishedProvider> publishedProviders = null, IEnumerable<string> providerIds = null)
        {
            string specificationId = prereqObject as string;

            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNotEmpty(providerIds, nameof(providerIds));

            await BasePerformChecks(
                specificationId, 
                specificationId, 
                jobId, 
                new string[] 
                { 
                    JobConstants.DefinitionNames.RefreshFundingJob, 
                    JobConstants.DefinitionNames.PublishAllProviderFundingJob,
                    JobConstants.DefinitionNames.PublishBatchProviderFundingJob,
                    JobConstants.DefinitionNames.ReIndexPublishedProvidersJob,
                    JobConstants.DefinitionNames.ApproveAllProviderFundingJob
                });
        }

        protected override Task<IEnumerable<string>> PerformChecks<T>(T prereqObject, IEnumerable<PublishedProvider> publishedProviders = null)
        {
            return Task.FromResult<IEnumerable<string>>(null);
        }

        public override bool IsCheckerType(PrerequisiteCheckerType type)
        {
            return type == PrerequisiteCheckerType.ApproveBatchProviders;
        }
    }
}