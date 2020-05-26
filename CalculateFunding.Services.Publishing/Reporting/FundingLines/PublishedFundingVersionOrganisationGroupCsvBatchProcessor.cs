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
    public class PublishedFundingVersionOrganisationGroupCsvBatchProcessor : CsvBatchProcessBase, IFundingLineCsvBatchProcessor
    {
        private readonly ISpecificationService _specificationService;
        private readonly AsyncPolicy _publishingResiliencePolicy;
        private readonly IPublishedFundingVersionDataService _publishedFundingVersionDataService;
        private readonly IPublishedFundingOrganisationGroupingService _publishedFundingOrganisationGroupingService;

        public PublishedFundingVersionOrganisationGroupCsvBatchProcessor(
            IPublishedFundingOrganisationGroupingService publishedFundingOrganisationGroupingService,
            ISpecificationService specificationService,
            IPublishedFundingVersionDataService publishedFundingVersionDataService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IFileSystemAccess fileSystemAccess,
            ICsvUtils csvUtils) 
            : base(fileSystemAccess, csvUtils)
        {
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(publishedFundingVersionDataService, nameof(publishedFundingVersionDataService));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishedFundingOrganisationGroupingService, nameof(publishedFundingOrganisationGroupingService));

            _specificationService = specificationService;
            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _publishedFundingVersionDataService = publishedFundingVersionDataService;
            _publishedFundingOrganisationGroupingService = publishedFundingOrganisationGroupingService;
        }

        public bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return jobType == FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues;
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

            IEnumerable<PublishedFundingVersion> publishedFundingVersions = await _publishingResiliencePolicy.ExecuteAsync(() =>
                _publishedFundingVersionDataService.GetPublishedFundingVersion(fundingStreamId, fundingPeriodId));

            IEnumerable<PublishedFundingOrganisationGrouping> organisationGroupings = 
                await _publishedFundingOrganisationGroupingService.GeneratePublishedFundingOrganisationGrouping(true, fundingStreamId, specification, publishedFundingVersions);

            IEnumerable<PublishedFundingOrganisationGrouping> publishingOrganisationGroupings = organisationGroupings
                .OrderBy(_ => _.OrganisationGroupResult.GroupTypeCode.ToString());

            foreach (PublishedFundingOrganisationGrouping publishedFundingOrganisationGrouping in publishingOrganisationGroupings)
            {
                publishedFundingOrganisationGrouping.PublishedFundingVersions = publishedFundingOrganisationGrouping.PublishedFundingVersions.OrderByDescending(_ => _.Date);
            }

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
