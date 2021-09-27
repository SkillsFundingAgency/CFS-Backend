using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedProviderVersionCsvBatchProcessor : PublishedProviderCsvBatchProcessor
    {
        public PublishedProviderVersionCsvBatchProcessor(IPublishedFundingRepository publishedFunding,
            IPublishedFundingPredicateBuilder predicateBuilder,
            IPublishingResiliencePolicies resiliencePolicies,
            IFileSystemAccess fileSystemAccess,
            IProfilingService profilingService,
            ICsvUtils csvUtils,
            IPoliciesService policiesService) : base(publishedFunding, 
                                        predicateBuilder, 
                                        resiliencePolicies, 
                                        profilingService, 
                                        fileSystemAccess, 
                                        csvUtils,
                                        policiesService)
        {
        }

        public override bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return jobType == FundingLineCsvGeneratorJobType.History ||
                   jobType == FundingLineCsvGeneratorJobType.HistoryProfileValues;
        }

        public override async Task<bool> GenerateCsv(FundingLineCsvGeneratorJobType jobType,
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

            string predicate = _predicateBuilder.BuildPredicate(jobType);
            string join = _predicateBuilder.BuildJoinPredicate(jobType);

            IEnumerable<string> distinctFundingLineNames = await _policiesService.GetDistinctFundingLineNames(fundingStreamId, fundingPeriodId);

            IEnumerable<ProfilePeriodPattern> uniqueProfilePatterns = await GetProfilePeriodPatterns(jobType, fundingStreamId, fundingPeriodId, fundingLineCode);

            using ICosmosDbFeedIterator documents = _publishedFunding.GetPublishedProviderVersionsForBatchProcessing(predicate,
                specificationId,
                BatchSize,
                join,
                fundingLineName);

            if (documents == null)
            {
                throw new NonRetriableException(
                    $"Unable to generate CSV for PublishedProviderVersionCsvBatchProcessor for specification {specificationId}. Failed to get feed iterator from cosmos");
            }

            while (documents.HasMoreResults)
            {
                IEnumerable<PublishedProviderVersion> publishedProviderVersions = (await documents.ReadNext<PublishedProviderVersion>()).Where(_ => _.FundingLines.AnyWithNullCheck());

                IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(
                    publishedProviderVersions, 
                    jobType, 
                    uniqueProfilePatterns,
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