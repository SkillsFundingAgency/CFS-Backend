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

namespace CalculateFunding.Services.Publishing
{
    public class RefreshPrerequisiteChecker : BasePrerequisiteChecker, IPrerequisiteChecker
    {
        private readonly ISpecificationFundingStatusService _specificationFundingStatusService;
        private readonly ISpecificationService _specificationService;
        private readonly ICalculationPrerequisiteCheckerService _calculationApprovalCheckerService;
        private readonly ILogger _logger;
        private readonly IPoliciesService _policiesService;
        private readonly IProfilingService _profilingService;
        private readonly ICalculationsService _calculationsService;
        private readonly AsyncPolicy _fundingStreamPaymentDatesRepositoryPolicy;
        private readonly IFundingStreamPaymentDatesRepository _fundingStreamPaymentDatesRepository;

        public RefreshPrerequisiteChecker(
            ISpecificationFundingStatusService specificationFundingStatusService,
            ISpecificationService specificationService,
            IJobsRunning jobsRunning,
            ICalculationPrerequisiteCheckerService calculationApprovalCheckerService,
            IJobManagement jobManagement,
            ILogger logger,
            IPoliciesService policiesService,
            IProfilingService profilingService,
            ICalculationsService calculationsService,
            IPublishingResiliencePolicies resiliencePolicies,
            IFundingStreamPaymentDatesRepository fundingStreamPaymentDatesRepository) : base(jobsRunning, jobManagement, logger)
        {
            Guard.ArgumentNotNull(specificationFundingStatusService, nameof(specificationFundingStatusService));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(calculationApprovalCheckerService, nameof(calculationApprovalCheckerService));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(profilingService, nameof(profilingService));
            Guard.ArgumentNotNull(calculationsService, nameof(calculationsService));
            Guard.ArgumentNotNull(resiliencePolicies?.FundingStreamPaymentDatesRepository, nameof(resiliencePolicies.FundingStreamPaymentDatesRepository));
            Guard.ArgumentNotNull(fundingStreamPaymentDatesRepository, nameof(fundingStreamPaymentDatesRepository));

            _specificationFundingStatusService = specificationFundingStatusService;
            _specificationService = specificationService;
            _calculationApprovalCheckerService = calculationApprovalCheckerService;
            _logger = logger;
            _policiesService = policiesService;
            _profilingService = profilingService;
            _calculationsService = calculationsService;
            _fundingStreamPaymentDatesRepository = fundingStreamPaymentDatesRepository;

            _fundingStreamPaymentDatesRepositoryPolicy = resiliencePolicies.FundingStreamPaymentDatesRepository;
        }

        public async Task PerformChecks<T>(T prereqObject, string jobId, IEnumerable<PublishedProvider> publishedProviders = null, IEnumerable<Provider> providers = null)
        {
            SpecificationSummary specification = prereqObject as SpecificationSummary;

            Guard.ArgumentNotNull(specification, nameof(specification));
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));

            await BasePerformChecks(specification, specification.Id, jobId, new string[]
            {
                JobConstants.DefinitionNames.CreateInstructAllocationJob,
                JobConstants.DefinitionNames.ApproveAllProviderFundingJob,
                JobConstants.DefinitionNames.ApproveBatchProviderFundingJob,
                JobConstants.DefinitionNames.PublishAllProviderFundingJob,
                JobConstants.DefinitionNames.PublishBatchProviderFundingJob,
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
            SpecificationSummary specification = prereqObject as SpecificationSummary;

            Guard.ArgumentNotNull(specification, nameof(specification));

            if (specification.ApprovalStatus != PublishStatus.Approved)
            {
                _logger.Error("Specification failed refresh prerequisite check. Reason: must be approved");
                return new string[] { "Specification must be approved." };
            }

            if (providers.IsNullOrEmpty())
            {
                _logger.Error("Specification failed refresh prerequisite check. Reason: no scoped providers");
                return new string[] { "Specification must have providers in scope." };
            }

            IEnumerable<ObsoleteItem> obsoleteItems = await _calculationsService.GetObsoleteItemsForSpecification(specification.Id);

            if (obsoleteItems.AnyWithNullCheck())
            {
                string errorMessage = "Funding can not be refreshed due to calculation errors.";

                _logger.Error(errorMessage);
                return new string[] { errorMessage };
            }

            SpecificationFundingStatus specificationFundingStatus = await _specificationFundingStatusService.CheckChooseForFundingStatus(specification);

            if (specificationFundingStatus == SpecificationFundingStatus.SharesAlreadyChosenFundingStream)
            {
                string errorMessage = $"Specification with id: '{specification.Id} already shares chosen funding streams";

                _logger.Error(errorMessage);
                return new string[] { errorMessage };
            }

            foreach (Reference fundingStream in specification.FundingStreams) 
            {
                IEnumerable<Common.ApiClient.Profiling.Models.FundingStreamPeriodProfilePattern> fundingStreamPeriodProfilePatterns 
                    = await _profilingService.GetProfilePatternsForFundingStreamAndFundingPeriod(fundingStream.Id, specification.FundingPeriod.Id);

                if (!fundingStreamPeriodProfilePatterns.AnyWithNullCheck())
                {
                    TemplateMetadataDistinctFundingLinesContents templateMetadataDistinctFundingLinesContents =
                        await _policiesService.GetDistinctTemplateMetadataFundingLinesContents(
                            fundingStream.Id, 
                            specification.FundingPeriod.Id, 
                            specification.TemplateIds[fundingStream.Id]);

                    IEnumerable<TemplateMetadataFundingLine> templateMetadataPaymentFundingLines
                        = templateMetadataDistinctFundingLinesContents
                            .FundingLines
                            .Where(_ => _.Type == Common.TemplateMetadata.Enums.FundingLineType.Payment);

                    string errorMessage = $"Profiling configuration missing for funding lines {string.Join(",", templateMetadataPaymentFundingLines.Select(_ => _.FundingLineCode))}";

                    _logger.Error(errorMessage);
                    return new string[] { errorMessage };
                }
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

                return results;
            }

            string fundingStreamId = specification.FundingStreams.FirstOrDefault()?.Id;

            FundingStreamPaymentDates fundingStreamPaymentDates = await _fundingStreamPaymentDatesRepositoryPolicy.ExecuteAsync(() =>
                _fundingStreamPaymentDatesRepository.GetUpdateDates(fundingStreamId, specification.FundingPeriod.Id));

            if (fundingStreamPaymentDates != null)
            {
                FundingStreamPaymentDate lastFundingStreamPaymentDate = fundingStreamPaymentDates.PaymentDates
                    .Where(_ => _.Type == ProfilePeriodType.CalendarMonth)
                    .OrderByDescending(_ => _.Year)
                    .ThenByDescending(_ => _.TypeValue.ToMonthNumber())
                    .ThenByDescending(_ => _.Occurrence)
                    .FirstOrDefault();

                IEnumerable<ApiProfileVariationPointer> variationPointers = await _specificationService.GetProfileVariationPointers(specification.Id);

                if (variationPointers != null && lastFundingStreamPaymentDate != null)
                {
                    IEnumerable<ApiProfileVariationPointer> fundingStreamVariationPointers = variationPointers
                        .Where(_ => _.FundingStreamId == fundingStreamId);

                    ApiProfileVariationPointer latestVariationPointer = fundingStreamVariationPointers
                        .Where(_ => _.PeriodType == ApiPeriodType.CalendarMonth.ToString())
                        .OrderByDescending(_ => _.Year)
                        .ThenByDescending(_ => _.TypeValue.ToMonthNumber())
                        .ThenByDescending(_ => _.Occurrence)
                        .FirstOrDefault();

                    if (latestVariationPointer.Year < lastFundingStreamPaymentDate?.Year
                        || (latestVariationPointer.Year == lastFundingStreamPaymentDate?.Year 
                            && latestVariationPointer.TypeValue.ToMonthNumber() < lastFundingStreamPaymentDate.TypeValue.ToMonthNumber()))
                    {
                        string errorMessage = $"There are payment funding lines with variation instalments set earlier than the current profile instalment.{Environment.NewLine}#VariationInstallmentLink# must be set later or equal to the current profile instalment.";

                        _logger.Error(errorMessage);
                        return new string[] { errorMessage };
                    }
                }
            }

            return results;
        }

        public override bool IsCheckerType(PrerequisiteCheckerType type)
        {
            return type == PrerequisiteCheckerType.Refresh;
        }
    }
}
