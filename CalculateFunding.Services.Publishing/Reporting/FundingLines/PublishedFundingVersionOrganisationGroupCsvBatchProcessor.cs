using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedFundingVersionOrganisationGroupCsvBatchProcessor : CsvBatchProcessBase, IFundingLineCsvBatchProcessor
    {
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly IPoliciesService _policiesService;

        public PublishedFundingVersionOrganisationGroupCsvBatchProcessor(IFileSystemAccess fileSystemAccess,
            ICsvUtils csvUtils,
            IPublishedFundingRepository publishedFunding,
            IPoliciesService policiesService) 
            : base(fileSystemAccess, csvUtils)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));

            _publishedFunding = publishedFunding;
            _policiesService = policiesService;
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
            string fundingLineName,
            string fundingStreamId,
            string fundingLineCode)
        {
            bool outputHeaders = true;
            bool processedResults = false;

            IEnumerable<string> distinctFundingLineNames = await _policiesService.GetDistinctFundingLineNames(fundingStreamId, fundingPeriodId);

            using ICosmosDbFeedIterator documents = _publishedFunding.GetPublishedFundingVersionsForBatchProcessing(specificationId,
                fundingStreamId,
                fundingPeriodId,
                BatchSize);

            if (documents == null)
            {
                throw new NonRetriableException(
                    $"Unable to generate CSV for PublishedFundingVersionOrganisationGroupCsvBatchProcessor for specification {specificationId}. Failed to get feed iterator from cosmos");
            }

            while (documents.HasMoreResults)
            {
                IEnumerable<PublishedFundingVersion> publishedFundingVersions = await documents.ReadNext<PublishedFundingVersion>();
                
                IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(
                    publishedFundingVersions, 
                    jobType,
                    distinctFundingLineNames: distinctFundingLineNames);

                if (AppendCsvFragment(temporaryFilePath, csvRows, outputHeaders))
                {
                    outputHeaders = false;
                    processedResults = true;
                } 
            }
            
            return processedResults;
        }
    }
}
