using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedProviderVersionCsvBatchProcessor : CsvBatchProcessBase,  IFundingLineCsvBatchProcessor
    {
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly IPublishedFundingPredicateBuilder _predicateBuilder;
        private readonly AsyncPolicy _publishedFundingRepository;

        public PublishedProviderVersionCsvBatchProcessor(IPublishedFundingRepository publishedFunding,
            IPublishedFundingPredicateBuilder predicateBuilder,
            IPublishingResiliencePolicies resiliencePolicies,
            IFileSystemAccess fileSystemAccess,
            ICsvUtils csvUtils) : base(fileSystemAccess, csvUtils)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(predicateBuilder, nameof(predicateBuilder));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            
            _publishedFunding = publishedFunding;
            _predicateBuilder = predicateBuilder;
            _publishedFundingRepository = resiliencePolicies.PublishedFundingRepository;
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

            await _publishedFundingRepository.ExecuteAsync(() => _publishedFunding.PublishedProviderVersionBatchProcessing(predicate,
                specificationId,
                publishedProviderVersions =>
                {
                    IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(publishedProviderVersions);

                    AppendCsvFragment(temporaryFilePath, csvRows, outputHeaders);

                    outputHeaders = false;
                    processedResults = true;
                    return Task.CompletedTask;
                }, 
                BatchSize,
                join,
                fundingLineCode)
            );
            
            return processedResults;
        }
    }
}