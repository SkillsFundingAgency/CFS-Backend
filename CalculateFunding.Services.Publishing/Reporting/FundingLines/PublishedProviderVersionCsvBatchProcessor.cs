using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling.Models;
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
        private readonly IProfilingService _profilingService;

        public PublishedProviderVersionCsvBatchProcessor(IPublishedFundingRepository publishedFunding,
            IPublishedFundingPredicateBuilder predicateBuilder,
            IFileSystemAccess fileSystemAccess,
            IProfilingService profilingService,
            ICsvUtils csvUtils) : base(fileSystemAccess, csvUtils)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(predicateBuilder, nameof(predicateBuilder));
            Guard.ArgumentNotNull(profilingService, nameof(profilingService));

            _publishedFunding = publishedFunding;
            _predicateBuilder = predicateBuilder;
            _profilingService = profilingService;
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
            string fundingLineName,
            string fundingStreamId,
            string fundingLineCode)
        {
            bool outputHeaders = true;
            bool processedResults = false;

            string predicate = _predicateBuilder.BuildPredicate(jobType);
            string join = _predicateBuilder.BuildJoinPredicate(jobType);

            IEnumerable<FundingStreamPeriodProfilePattern> profilePeriodPatterns = await _profilingService.GetProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId, fundingPeriodId);

            if (profilePeriodPatterns == null)
            {
                throw new NonRetriableException(
                    $"Did not locate any profile patterns for funding stream {fundingStreamId} and funding period {fundingPeriodId}. Unable to continue with Qa Schema Generation");
            }

            IEnumerable<ProfilePeriodPattern> allPatterns = profilePeriodPatterns
                    .Where(p => p.FundingLineId == fundingLineCode)
                    .SelectMany(p => p.ProfilePattern)
                    .ToArray();

            IEnumerable<ProfilePeriodPattern> uniqueProfilePatterns = allPatterns.DistinctBy(_ => new
            {
                _.Occurrence,
                _.Period,
                _.PeriodType,
                _.PeriodYear
            });

            ICosmosDbFeedIterator<PublishedProviderVersion> documents = _publishedFunding.GetPublishedProviderVersionsForBatchProcessing(predicate,
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
                IEnumerable<PublishedProviderVersion> publishedProviderVersions = (await documents.ReadNext()).Where(_ => _.FundingLines.AnyWithNullCheck());

                IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(publishedProviderVersions, jobType, uniqueProfilePatterns);

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