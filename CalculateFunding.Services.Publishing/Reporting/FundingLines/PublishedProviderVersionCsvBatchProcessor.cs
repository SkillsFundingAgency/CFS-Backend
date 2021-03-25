using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedProviderVersionCsvBatchProcessor : CsvBatchProcessBase,  IFundingLineCsvBatchProcessor
    {
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly IPublishedFundingPredicateBuilder _predicateBuilder;

        public PublishedProviderVersionCsvBatchProcessor(IPublishedFundingRepository publishedFunding,
            IPublishedFundingPredicateBuilder predicateBuilder,
            IFileSystemAccess fileSystemAccess,
            ICsvUtils csvUtils) : base(fileSystemAccess, csvUtils)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(predicateBuilder, nameof(predicateBuilder));
            
            _publishedFunding = publishedFunding;
            _predicateBuilder = predicateBuilder;
        }

        public bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return jobType == FundingLineCsvGeneratorJobType.History ||
                   jobType == FundingLineCsvGeneratorJobType.HistoryProfileValues;
        }

        public async Task<bool> GenerateCsv(FundingLineCsvGeneratorJobType jobType,
            string specificationId, 
            string fundingPeriodId,
            string temporaryFilePath, 
            IFundingLineCsvTransform fundingLineCsvTransform,
            string fundingLineCode,
            string fundingStreamId)
        {
            bool outputHeaders = true;
            bool processedResults = false;

            string predicate = _predicateBuilder.BuildPredicate(jobType);
            string join = _predicateBuilder.BuildJoinPredicate(jobType);

            ICosmosDbFeedIterator<PublishedProviderVersion> documents = _publishedFunding.GetPublishedProviderVersionsForBatchProcessing(predicate,
                specificationId,
                BatchSize,
                join,
                fundingLineCode);

            if (documents == null)
            {
                throw new NonRetriableException(
                    $"Unable to generate CSV for PublishedProviderVersionCsvBatchProcessor for specification {specificationId}. Failed to get feed iterator from cosmos");
            }

            while (documents.HasMoreResults)
            {
                var publishedProviderVersions = await documents.ReadNext();
                
                IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(publishedProviderVersions, jobType);

                AppendCsvFragment(temporaryFilePath, csvRows, outputHeaders);

                outputHeaders = false;
                processedResults = true;
            }
            
            return processedResults;
        }
    }
}