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
    public class PublishedFundingOrganisationGroupCsvBatchProcessor : CsvBatchProcessBase, IFundingLineCsvBatchProcessor
    {
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly IPoliciesService _policiesService;

        public PublishedFundingOrganisationGroupCsvBatchProcessor(IFileSystemAccess fileSystemAccess,
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
            return jobType == FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues;
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

            using ICosmosDbFeedIterator documents = _publishedFunding.GetPublishedFundingForBatchProcessing(specificationId,
                fundingStreamId,
                fundingPeriodId,
                BatchSize);

            if (documents == null)
            {
                throw new NonRetriableException(
                    $"Unable to generate CSV for PublishedFundingOrganisationGroupCsvBatchProcessor for specification {specificationId}. Failed to get feed iterator from cosmos");
            }

            while (documents.HasMoreResults)
            {
                IEnumerable<PublishedFunding> publishedFunding = await documents.ReadNext<PublishedFunding>();
                
                IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(
                    publishedFunding, 
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
