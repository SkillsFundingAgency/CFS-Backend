using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedProviderOrganisationGroupCsvBatchProcessor : CsvBatchProcessBase, IFundingLineCsvBatchProcessor
    {
        private readonly ISpecificationService _specificationService;
        private readonly Policy _publishingResiliencePolicy;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly IOrganisationGroupGenerator _organisationGroupGenerator;
        private readonly IPoliciesService _policiesService;
        private readonly IProviderService _providerService;
        private readonly IMapper _mapper;
        private readonly IPublishedFundingChangeDetectorService _publishedFundingChangeDetectorService;

        public PublishedProviderOrganisationGroupCsvBatchProcessor(
            ISpecificationService specificationService,
            IPublishedFundingDataService publishedFundingDataService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IOrganisationGroupGenerator organisationGroupGenerator,
            IProviderService providerService,
            IMapper mapper,
            IPublishedFundingChangeDetectorService publishedFundingChangeDetectorService,
            IPoliciesService policiesService,
            IFileSystemAccess fileSystemAccess,
            ICsvUtils csvUtils) 
            : base(fileSystemAccess, csvUtils)
        {
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(organisationGroupGenerator, nameof(organisationGroupGenerator));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(publishedFundingChangeDetectorService, nameof(publishedFundingChangeDetectorService));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));

            _specificationService = specificationService;
            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _publishedFundingDataService = publishedFundingDataService;
            _organisationGroupGenerator = organisationGroupGenerator;
            _providerService = providerService;
            _mapper = mapper;
            _publishedFundingChangeDetectorService = publishedFundingChangeDetectorService;
            _policiesService = policiesService;
        }

        public bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return jobType == FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues;
        }

        public async Task<bool> GenerateCsv(
            FundingLineCsvGeneratorJobType jobType, 
            string specificationId, 
            string temporaryFilePath, 
            IFundingLineCsvTransform fundingLineCsvTransform, 
            string fundingLineCode, 
            string fundingStreamId)
        {
            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);

            IEnumerable<PublishedFunding> publishedFunding = await _publishingResiliencePolicy.ExecuteAsync(() =>
                _publishedFundingDataService.GetCurrentPublishedFunding(fundingStreamId, specification.FundingPeriod.Id));

            FundingConfiguration fundingConfiguration = await _policiesService.GetFundingConfiguration(fundingStreamId, specification.FundingPeriod.Id);

            Reference fundingStream = new Reference { Id = fundingStreamId };
            (IDictionary<string, PublishedProvider> publishedProvidersForFundingStream, IDictionary<string, PublishedProvider> scopedPublishedProviders) = 
                await _providerService.GetPublishedProviders(fundingStream, specification);

            IEnumerable<Provider> scopedProviders = scopedPublishedProviders?.Values.Select(_ => _.Current.Provider);

            IEnumerable<OrganisationGroupResult> organisationGroups =
                await _organisationGroupGenerator.GenerateOrganisationGroup(fundingConfiguration, _mapper.Map<IEnumerable<ApiProvider>>(scopedProviders), specification.ProviderVersionId);

            IEnumerable<(PublishedFunding PublishedFunding, OrganisationGroupResult OrganisationGroupResult)> organisationGroupings =
                _publishedFundingChangeDetectorService.GenerateOrganisationGroupings(organisationGroups, publishedFunding, publishedProvidersForFundingStream);

            IEnumerable<PublishedFundingOrganisationGrouping> publishingOrganisationGroupings =
                organisationGroupings
                .OrderBy(x => x.PublishedFunding.Current.OrganisationGroupTypeCode)
                .Select(x=>new PublishedFundingOrganisationGrouping {PublishedFunding = x.PublishedFunding, OrganisationGroupResult = x.OrganisationGroupResult });

            IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(publishingOrganisationGroupings);
            AppendCsvFragment(temporaryFilePath, csvRows, outputHeaders:true);

            return true;
        }
    }
}
