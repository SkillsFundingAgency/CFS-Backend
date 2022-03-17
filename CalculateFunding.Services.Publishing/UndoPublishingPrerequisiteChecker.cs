using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;
using ApiProfileVariationPointer = CalculateFunding.Common.ApiClient.Specifications.Models.ProfileVariationPointer;
using ApiPeriodType = CalculateFunding.Common.ApiClient.Profiling.Models.PeriodType;
using CalculateFunding.Services.Core.Extensions;
using Polly;
using CalculateFunding.Common.ApiClient.Calcs.Models.ObsoleteItems;
using CalculateFunding.Services.Publishing.Undo;

namespace CalculateFunding.Services.Publishing
{
    public class UndoPublishingPrerequisiteChecker : BasePrerequisiteChecker, IPrerequisiteChecker
    {
        private readonly ISpecificationService _specificationService;
        private readonly ILogger _logger;

        public override string Name => "Undo Publishing";

        public UndoPublishingPrerequisiteChecker(
            IJobsRunning jobsRunning,
            IJobManagement jobManagement,
            ISpecificationService specificationService,
            ILogger logger) : base(jobsRunning, jobManagement, logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));

            _logger = logger;
            _specificationService = specificationService;
        }

        public async Task PerformChecks<T>(T prereqObject, string jobId, IEnumerable<PublishedProvider> publishedProviders = null, IEnumerable<Provider> providers = null)
        {
            UndoTaskDetails taskDetails = prereqObject as UndoTaskDetails;

            Guard.ArgumentNotNull(taskDetails, nameof(taskDetails));

            await BasePerformChecks(taskDetails, taskDetails.SpecificationId, jobId, new string[]
            {
                JobConstants.DefinitionNames.RefreshFundingJob,
                JobConstants.DefinitionNames.ApproveAllProviderFundingJob,
                JobConstants.DefinitionNames.ApproveBatchProviderFundingJob,
                JobConstants.DefinitionNames.PublishAllProviderFundingJob,
                JobConstants.DefinitionNames.PublishBatchProviderFundingJob,
                JobConstants.DefinitionNames.ReleaseProvidersToChannelsJob,
                JobConstants.DefinitionNames.ReIndexPublishedProvidersJob,
                JobConstants.DefinitionNames.GenerateGraphAndInstructAllocationJob,
                JobConstants.DefinitionNames.GenerateGraphAndInstructGenerateAggregationAllocationJob
            }, publishedProviders, providers);
        }

        protected override async Task<IEnumerable<string>> PerformChecks<T>(
            T prereqObject, 
            IEnumerable<PublishedProvider> publishedProviders, 
            IEnumerable<Provider> providers)
        {
            UndoTaskDetails taskDetails = prereqObject as UndoTaskDetails;
            
            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(taskDetails.SpecificationId);

            string errorMessage;

            if (specification == null)
            {
                errorMessage = $"Could not find specification with id '{taskDetails.SpecificationId}'";
                _logger.Error(errorMessage);
                return new string[] { errorMessage };
            }

            if (specification.FundingPeriod.Id != taskDetails.FundingPeriodId)
            {
                errorMessage = $"Specification failed undo publishimg prerequisite check. Reason: funding period {specification.FundingPeriod.Id} doesn't match correlation {taskDetails.FundingPeriodId}";
                _logger.Error(errorMessage);
                return new string[] { errorMessage };
            }

            if (specification.FundingStreams.First().Id != taskDetails.FundingStreamId)
            {
                errorMessage = $"Specification failed undo publishimg prerequisite check. Reason: funding stream {specification.FundingStreams.First().Id} doesn't match correlation {taskDetails.FundingStreamId}";
                _logger.Error(errorMessage);
                return new string[] { errorMessage };
            }

            return await Task.FromResult(ArraySegment<string>.Empty);
        }

        public override bool IsCheckerType(PrerequisiteCheckerType type)
        {
            return type == PrerequisiteCheckerType.UndoPublishing;
        }
    }
}
