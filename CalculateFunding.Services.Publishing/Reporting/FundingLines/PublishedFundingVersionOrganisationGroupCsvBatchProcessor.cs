using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedFundingVersionOrganisationGroupCsvBatchProcessor : CsvBatchProcessBase, IFundingLineCsvBatchProcessor
    {
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly AsyncPolicy _publishedFundingPolicy;

        public PublishedFundingVersionOrganisationGroupCsvBatchProcessor(IPublishingResiliencePolicies publishingResiliencePolicies,
            IFileSystemAccess fileSystemAccess,
            ICsvUtils csvUtils,
            IPublishedFundingRepository publishedFunding) 
            : base(fileSystemAccess, csvUtils)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));

            _publishedFundingPolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _publishedFunding = publishedFunding;
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
            bool outputHeaders = true;
            bool processedResults = false;

            await _publishedFundingPolicy.ExecuteAsync(() =>  _publishedFunding.PublishedFundingVersionBatchProcessing(specificationId,
                fundingStreamId,
                fundingPeriodId,
                publishedFundingVersions =>
                {
                    IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(publishedFundingVersions);
                        
                    AppendCsvFragment(temporaryFilePath, csvRows, outputHeaders);

                    outputHeaders = false;
                    processedResults = true;
                        
                    return Task.CompletedTask;
                },
                BatchSize));
            
            return processedResults;
        }
    }
}
