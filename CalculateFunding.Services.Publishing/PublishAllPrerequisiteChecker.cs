using System;
using System.Collections.Generic;
using System.Linq;
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
    public class PublishAllPrerequisiteChecker : BasePrerequisiteChecker, IPrerequisiteChecker
    {
        private readonly ISpecificationFundingStatusService _specificationFundingStatusService;
        private readonly ILogger _logger;

        public PublishAllPrerequisiteChecker(
            ISpecificationFundingStatusService specificationFundingStatusService,
            IJobsRunning jobsRunning,
            IJobManagement jobManagement,
            ILogger logger) : base(jobsRunning, jobManagement, logger)
        {
            Guard.ArgumentNotNull(specificationFundingStatusService, nameof(specificationFundingStatusService));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _specificationFundingStatusService = specificationFundingStatusService;
            _logger = logger;
        }

        public virtual async Task PerformChecks<TSpecification>(TSpecification prereqObject, string jobId, IEnumerable<PublishedProvider> publishedProviders = null, IEnumerable<Provider> providers = null)
        {
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));
            SpecificationSummary specification = prereqObject as SpecificationSummary;
            Guard.ArgumentNotNull(specification, nameof(specification));

            await BasePerformChecks(prereqObject, specification.Id, jobId, new string[]
            { 
                JobConstants.DefinitionNames.RefreshFundingJob, 
                JobConstants.DefinitionNames.ApproveAllProviderFundingJob,
                JobConstants.DefinitionNames.ApproveBatchProviderFundingJob,
                JobConstants.DefinitionNames.ReIndexPublishedProvidersJob,
                JobConstants.DefinitionNames.PublishBatchProviderFundingJob
            }, publishedProviders);
        }

        protected async override Task<IEnumerable<string>> PerformChecks<TSpecification>(TSpecification prereqObject, IEnumerable<PublishedProvider> publishedProviders = null, IEnumerable<Provider> providers = null)
        {
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));

            SpecificationSummary specification = prereqObject as SpecificationSummary;

            Guard.ArgumentNotNull(specification, nameof(specification));

            SpecificationFundingStatus specificationFundingStatus = await _specificationFundingStatusService.CheckChooseForFundingStatus(specification);

            if (specificationFundingStatus != SpecificationFundingStatus.AlreadyChosen)
            {
                string errorMessage = $"Specification with id: '{specification.Id}' is not chosen for funding";

                _logger.Error(errorMessage);
                return new string[] { errorMessage };
            }

            List<string> results = new List<string>();

            if (publishedProviders?.Any(_ => _.Current.Status == PublishedProviderStatus.Draft || _.Current.Status == PublishedProviderStatus.Updated) ?? false)
            {
                results.AddRange(publishedProviders.Where(_ => _.Current.Status == PublishedProviderStatus.Draft || _.Current.Status == PublishedProviderStatus.Updated).Select(_ => $"Provider with id:{_.Id} has current status:{_.Current.Status} so cannot be published."));
                _logger.Error(string.Join(Environment.NewLine, results));
                return results;
            }

            return results;
        }
    
        public override bool IsCheckerType(PrerequisiteCheckerType type)
        {
            return type == PrerequisiteCheckerType.ReleaseAllProviders;
        }
    }
}
