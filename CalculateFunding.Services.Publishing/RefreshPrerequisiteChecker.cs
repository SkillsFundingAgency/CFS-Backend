using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;
using Polly;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;

namespace CalculateFunding.Services.Publishing
{
    public class RefreshPrerequisiteChecker : BasePrerequisiteChecker, IPrerequisiteChecker
    {
        private readonly ISpecificationFundingStatusService _specificationFundingStatusService;
        private readonly ISpecificationService _specificationService;
        private readonly ICalculationPrerequisiteCheckerService _calculationApprovalCheckerService;
        private readonly ILogger _logger;
        private readonly IOrganisationGroupGenerator _organisationGroupGenerator;
        private readonly IMapper _mapper;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly AsyncPolicy _publishingResiliencePolicy;
        private readonly AsyncPolicy _policiesResiliencePolicy;
        private readonly IPoliciesService _policiesService;

        public RefreshPrerequisiteChecker(
            ISpecificationFundingStatusService specificationFundingStatusService,
            ISpecificationService specificationService,
            IJobsRunning jobsRunning,
            ICalculationPrerequisiteCheckerService calculationApprovalCheckerService,
            IJobManagement jobManagement,
            ILogger logger,
            IOrganisationGroupGenerator organisationGroupGenerator,
            IPoliciesService policiesService,
            IMapper mapper,
            IPublishedFundingDataService publishedFundingDataService,
            IPublishingResiliencePolicies publishingResiliencePolicies) : base(jobsRunning, jobManagement, logger)
        {
            Guard.ArgumentNotNull(specificationFundingStatusService, nameof(specificationFundingStatusService));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(calculationApprovalCheckerService, nameof(calculationApprovalCheckerService));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(organisationGroupGenerator, nameof(organisationGroupGenerator));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));

            _specificationFundingStatusService = specificationFundingStatusService;
            _specificationService = specificationService;
            _calculationApprovalCheckerService = calculationApprovalCheckerService;
            _logger = logger;
            _organisationGroupGenerator = organisationGroupGenerator;
            _policiesService = policiesService;
            _mapper = mapper;
            _publishedFundingDataService = publishedFundingDataService;
            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _policiesResiliencePolicy = publishingResiliencePolicies.PoliciesApiClient;
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

        protected override async Task<IEnumerable<string>> PerformChecks<T>(T prereqObject, IEnumerable<PublishedProvider> publishedProviders, IEnumerable<Provider> providers)
        {
            SpecificationSummary specification = prereqObject as SpecificationSummary;

            Guard.ArgumentNotNull(specification, nameof(specification));

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
                results.AddRange(calculationPrereqValidationErrors);
            }

            IEnumerable<string> checkTrustIdMismatch = await CheckTrustIdMismatch(specification, publishedProviders, providers);
            if (checkTrustIdMismatch.AnyWithNullCheck())
            {
                results.AddRange(checkTrustIdMismatch);
            }

            if (results.Any())
            {
                _logger.Error(string.Join(Environment.NewLine, results));
            }

            return results;
        }

        private async Task<IEnumerable<string>> CheckTrustIdMismatch(SpecificationSummary specification, IEnumerable<PublishedProvider> publishedProviders, IEnumerable<Provider> providers)
        {
            List<string> validationErrors = new List<string>();

            static string OrganisationGroupsKey(string fundingStreamId, string fundingPeriodId) => $"{fundingStreamId}:{fundingPeriodId}";
            IDictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData = new Dictionary<string, IEnumerable<OrganisationGroupResult>>();

            IEnumerable<Common.ApiClient.Providers.Models.Provider> apiClientProviders = _mapper.Map<IEnumerable<Common.ApiClient.Providers.Models.Provider>>(providers);
            IList<PublishedFunding> paymentPublishedFundings = (await _publishingResiliencePolicy.ExecuteAsync(() => _publishedFundingDataService.GetCurrentPublishedFunding(specification.Id)))
                                                                .Where(x => x.Current.GroupingReason == CalculateFunding.Models.Publishing.GroupingReason.Payment).ToList();

            foreach (PublishedProvider publishedProvider in publishedProviders)
            {
                IEnumerable<OrganisationGroupResult> organisationGroups;
                string keyForOrganisationGroups = OrganisationGroupsKey(publishedProvider.Current.FundingStreamId, publishedProvider.Current.FundingPeriodId);

                if (organisationGroupResultsData.ContainsKey(keyForOrganisationGroups))
                {
                    organisationGroups = organisationGroupResultsData[keyForOrganisationGroups];
                }
                else
                {
                    FundingConfiguration fundingConfiguration = await _policiesResiliencePolicy.ExecuteAsync(() => _policiesService.GetFundingConfiguration(publishedProvider.Current.FundingStreamId, publishedProvider.Current.FundingPeriodId));
                    organisationGroups = await _organisationGroupGenerator.GenerateOrganisationGroup(fundingConfiguration, apiClientProviders, specification.ProviderVersionId);
                    organisationGroupResultsData.Add(keyForOrganisationGroups, organisationGroups);
                }

                IEnumerable<PublishedFunding> unmatchedPublishedFundings = paymentPublishedFundings
                                                                        .Where(x => organisationGroups.Any(t => t.Identifiers.Any(i => x.Current.OrganisationGroupIdentifierValue == i.Value && x.Current.OrganisationGroupTypeIdentifier == i.Type.ToString()))
                                                                               && !x.Current.ProviderFundings.Any(pv => pv == publishedProvider.Released.FundingId))
                                                                        .ToList();

                if (unmatchedPublishedFundings.Any())
                {
                    validationErrors.Add($"TrustIds {string.Join(",", unmatchedPublishedFundings.Select(x => $"{x.Current.OrganisationGroupTypeIdentifier}-{x.Current.OrganisationGroupIdentifierValue}"))} not matched.");
                }
            }

            return validationErrors;
        }

        public override bool IsCheckerType(PrerequisiteCheckerType type)
        {
            return type == PrerequisiteCheckerType.Refresh;
        }
    }
}
