using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Polly;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedGroupsCsvBatchProcessor : CsvBatchProcessBase, IFundingLineCsvBatchProcessor
    {
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly AsyncPolicy _publishedFundingPolicy;

        public PublishedGroupsCsvBatchProcessor(IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies resiliencePolicies,
            IFileSystemAccess fileSystemAccess,
            ICsvUtils csvUtils) : base(fileSystemAccess, csvUtils)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));

            _publishedFundingRepository = publishedFundingRepository;
            _publishedFundingPolicy = resiliencePolicies.PublishedFundingRepository;
        }

        public bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return jobType == FundingLineCsvGeneratorJobType.PublishedGroups;
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

            await _publishedFundingPolicy.ExecuteAsync(() => _publishedFundingRepository.PublishedGroupBatchProcessing(
                specificationId,
                async publishedFundings =>
                {
                    List<PublishedFundingWithProvider> publishedfundingsWithProviders = new List<PublishedFundingWithProvider>();
                    
                    foreach (PublishedFunding publishedFunding in publishedFundings)
                    {
                        IEnumerable<PublishedProvider> providers = Enumerable.Empty<PublishedProvider>();
                        
                        if (publishedFunding.Current.ProviderFundings.Any())
                        {
                            providers = await _publishedFundingRepository.QueryPublishedProvider(specificationId, publishedFunding.Current.ProviderFundings);
                        }

                        publishedfundingsWithProviders.Add(new PublishedFundingWithProvider { PublishedFunding = publishedFunding, PublishedProviders = providers });
                    }

                    IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(publishedfundingsWithProviders);

                    AppendCsvFragment(temporaryFilePath, csvRows, outputHeaders);

                    outputHeaders = false;
                    processedResults = true;
                }, BatchSize)
            );

            return processedResults;

        }
    }
}