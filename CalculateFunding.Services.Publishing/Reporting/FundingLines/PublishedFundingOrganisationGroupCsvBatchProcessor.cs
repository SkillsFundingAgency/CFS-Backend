using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedFundingOrganisationGroupCsvBatchProcessor : CsvBatchProcessBase, IFundingLineCsvBatchProcessor
    {
        private readonly ISpecificationService _specificationService;
        private readonly AsyncPolicy _publishingResiliencePolicy;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly IPublishedFundingOrganisationGroupingService _publishedFundingOrganisationGroupingService;

        public PublishedFundingOrganisationGroupCsvBatchProcessor(
            IPublishedFundingOrganisationGroupingService publishedFundingOrganisationGroupingService,
            ISpecificationService specificationService,
            IPublishedFundingDataService publishedFundingDataService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IFileSystemAccess fileSystemAccess,
            ICsvUtils csvUtils) 
            : base(fileSystemAccess, csvUtils)
        {
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishedFundingOrganisationGroupingService, nameof(publishedFundingOrganisationGroupingService));

            _specificationService = specificationService;
            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _publishedFundingDataService = publishedFundingDataService;
            _publishedFundingOrganisationGroupingService = publishedFundingOrganisationGroupingService;
        }

        public bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return jobType == FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues;
        }

        public async Task<bool> GenerateCsv(
            FundingLineCsvGeneratorJobType jobType, 
            string specificationId, 
            string fundingPeriodId,
            string temporaryFilePath, 
            IFundingLineCsvTransform fundingLineCsvTransform, 
            string fundingLineCode,
            string fundingStreamId)
        {
            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);

            IEnumerable<PublishedFunding> publishedFunding = await _publishingResiliencePolicy.ExecuteAsync(() =>
                _publishedFundingDataService.GetCurrentPublishedFunding(fundingStreamId, fundingPeriodId));
            IEnumerable<PublishedFundingVersion> publishedFundingVersions = publishedFunding.Select(_ => _.Current);

            IEnumerable<PublishedFundingOrganisationGrouping> organisationGroupings = 
                await _publishedFundingOrganisationGroupingService.GeneratePublishedFundingOrganisationGrouping(false, fundingStreamId, specification, publishedFundingVersions);

            IEnumerable<PublishedFundingOrganisationGrouping> publishingOrganisationGroupings =
                organisationGroupings
                .OrderBy(_ => _.OrganisationGroupResult.GroupTypeCode.ToString());

            IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(publishingOrganisationGroupings);

            if (!csvRows.Any())
            {
                return false;
            }

            AppendCsvFragment(temporaryFilePath, csvRows, outputHeaders: true);
            
            return true;
        }
    }
}
