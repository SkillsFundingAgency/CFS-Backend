using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedProviderCsvBatchProcessor : CsvBatchProcessBase, IFundingLineCsvBatchProcessor
    {
        protected readonly IPublishedFundingRepository _publishedFunding;
        protected readonly IPublishedFundingPredicateBuilder _predicateBuilder;
        protected readonly IProfilingService _profilingService;
        protected readonly AsyncPolicy _publishedFundingRepository;

        public PublishedProviderCsvBatchProcessor(IPublishedFundingRepository publishedFunding,
            IPublishedFundingPredicateBuilder predicateBuilder,
            IPublishingResiliencePolicies resiliencePolicies,
            IProfilingService profilingService,
            IFileSystemAccess fileSystemAccess,
            ICsvUtils csvUtils) : base(fileSystemAccess, csvUtils)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(predicateBuilder, nameof(predicateBuilder));
            Guard.ArgumentNotNull(profilingService, nameof(profilingService));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            
            _publishedFunding = publishedFunding;
            _predicateBuilder = predicateBuilder;
            _profilingService = profilingService;
            _publishedFundingRepository = resiliencePolicies.PublishedFundingRepository;
        }

        public virtual bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return jobType == FundingLineCsvGeneratorJobType.Released ||
                   jobType == FundingLineCsvGeneratorJobType.CurrentState ||
                   jobType == FundingLineCsvGeneratorJobType.CurrentProfileValues;
        }

        public virtual async Task<bool> GenerateCsv(FundingLineCsvGeneratorJobType jobType,
            string specificationId, 
            string fundingPeriodId,
            string temporaryFilePath, 
            IFundingLineCsvTransform fundingLineCsvTransform,
            string fundingLineName,
            string fundingStreamId,
            string fundingLineCode)
        {
            bool outputHeader = true;
            bool processedResults = false;

            string predicate = _predicateBuilder.BuildPredicate(jobType);
            string joinPredicate = _predicateBuilder.BuildJoinPredicate(jobType);

            IEnumerable<ProfilePeriodPattern> uniqueProfilePatterns = await GetProfilePeriodPatterns(jobType, fundingStreamId, fundingPeriodId, fundingLineCode);

            await _publishedFundingRepository.ExecuteAsync(() => _publishedFunding.PublishedProviderBatchProcessing(predicate,
                specificationId,
                publishedProviders =>
                {
                    IEnumerable<PublishedProvider> publishedProvidersFiltered = publishedProviders.Where(_ => _.Current != null && _.Current.FundingLines.AnyWithNullCheck());

                    IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(publishedProvidersFiltered, jobType, uniqueProfilePatterns);

                    if (AppendCsvFragment(temporaryFilePath, csvRows, outputHeader))
                    {
                        outputHeader = false;
                        processedResults = true;
                    }

                    return Task.CompletedTask;
                }, BatchSize, joinPredicate, fundingLineName)
            );
            
            return processedResults;
        }

        protected async Task<IEnumerable<ProfilePeriodPattern>> GetProfilePeriodPatterns(FundingLineCsvGeneratorJobType jobType, string fundingStreamId, string fundingPeriodId, string fundingLineCode)
        {
            if (jobType != FundingLineCsvGeneratorJobType.CurrentProfileValues && jobType != FundingLineCsvGeneratorJobType.HistoryProfileValues)
            {
                return null;
            }

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

            return allPatterns.DistinctBy(_ => new
            {
                _.Occurrence,
                _.Period,
                _.PeriodType,
                _.PeriodYear
            });
        }
    }
}