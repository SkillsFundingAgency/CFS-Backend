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
    public class PublishedFundingOrganisationGroupCsvBatchProcessor : CsvBatchProcessBase, IFundingLineCsvBatchProcessor
    {
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly AsyncPolicy _publishedFundingPolicy;

        public PublishedFundingOrganisationGroupCsvBatchProcessor(IPublishingResiliencePolicies publishingResiliencePolicies,
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
            bool outputHeaders = true;
            bool processedResults = false;

            await _publishedFundingPolicy.ExecuteAsync(() =>  _publishedFunding.PublishedFundingBatchProcessing(specificationId,
                    fundingStreamId,
                    fundingPeriodId,
                    publishedFunding =>
                    {
                        IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(publishedFunding);
                        
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
