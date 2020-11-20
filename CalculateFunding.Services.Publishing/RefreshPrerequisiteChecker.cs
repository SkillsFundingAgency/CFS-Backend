using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class RefreshPrerequisiteChecker : BasePrerequisiteChecker, IPrerequisiteChecker
    {
        private readonly ISpecificationFundingStatusService _specificationFundingStatusService;
        private readonly ISpecificationService _specificationService;
        private readonly ICalculationPrerequisiteCheckerService _calculationApprovalCheckerService;
        private readonly ILogger _logger;

        public RefreshPrerequisiteChecker(
            ISpecificationFundingStatusService specificationFundingStatusService,
            ISpecificationService specificationService,
            IJobsRunning jobsRunning,
            ICalculationPrerequisiteCheckerService calculationApprovalCheckerService,
            IJobManagement jobManagement,
            ILogger logger) : base(jobsRunning, jobManagement, logger)
        {
            Guard.ArgumentNotNull(specificationFundingStatusService, nameof(specificationFundingStatusService));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(calculationApprovalCheckerService, nameof(calculationApprovalCheckerService));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _specificationFundingStatusService = specificationFundingStatusService;
            _specificationService = specificationService;
            _calculationApprovalCheckerService = calculationApprovalCheckerService;
            _logger = logger;
        }

        public async Task PerformChecks<T>(T prereqObject, string jobId, IEnumerable<PublishedProvider> publishedProviders = null, IEnumerable<Provider> providers = null)
        {
            SpecificationSummary specification = prereqObject as SpecificationSummary;

            Guard.ArgumentNotNull(specification, nameof(specification));
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));
            Guard.ArgumentNotNull(providers, nameof(providers));

            await BasePerformChecks(specification, specification.Id, jobId, new string[]
            {
                JobConstants.DefinitionNames.CreateInstructAllocationJob,
                JobConstants.DefinitionNames.ApproveAllProviderFundingJob,
                JobConstants.DefinitionNames.ApproveBatchProviderFundingJob,
                JobConstants.DefinitionNames.PublishAllProviderFundingJob,
                JobConstants.DefinitionNames.PublishBatchProviderFundingJob,
                JobConstants.DefinitionNames.ReIndexPublishedProvidersJob
            }, publishedProviders, providers);
        }

        protected override async Task<IEnumerable<string>> PerformChecks<T>(
            T prereqObject, 
            IEnumerable<PublishedProvider> publishedProviders, 
            IEnumerable<Provider> providers)
        {
            SpecificationSummary specification = prereqObject as SpecificationSummary;

            Guard.ArgumentNotNull(specification, nameof(specification));

            if (specification.ApprovalStatus != Common.ApiClient.Models.PublishStatus.Approved)
            {
                _logger.Error("Specification failed refresh prerequisite check. Reason: must be approved");
                return new string[] { "Specification must be approved." };
            }

            if (providers.IsNullOrEmpty())
            {
                _logger.Error("Specification failed refresh prerequisite check. Reason: no scoped providers");
                return new string[] { "Specification must have providers in scope." };
            }

            SpecificationFundingStatus specificationFundingStatus = await _specificationFundingStatusService.CheckChooseForFundingStatus(specification);

            if (specificationFundingStatus == SpecificationFundingStatus.SharesAlreadyChosenFundingStream)
            {
                string errorMessage = $"Specification with id: '{specification.Id} already shares chosen funding streams";

                _logger.Error(errorMessage);
                return new string[] { errorMessage };
            }

            if (specificationFundingStatus == SpecificationFundingStatus.CanChoose)
            {
                try
                {
                    await _specificationService.SelectSpecificationForFunding(specification.Id);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                    return new string[] { ex.Message };
                }
            }

            List<string> results = new List<string>();

            IEnumerable<string> calculationPrereqValidationErrors = await _calculationApprovalCheckerService.VerifyCalculationPrerequisites(specification);
            if (calculationPrereqValidationErrors.AnyWithNullCheck())
            {
                results.Add("All template calculations for this specification must be approved.");
                _logger.Error(string.Join(Environment.NewLine, calculationPrereqValidationErrors));
            }

            return results;
        }

        public override bool IsCheckerType(PrerequisiteCheckerType type)
        {
            return type == PrerequisiteCheckerType.Refresh;
        }
    }
}
