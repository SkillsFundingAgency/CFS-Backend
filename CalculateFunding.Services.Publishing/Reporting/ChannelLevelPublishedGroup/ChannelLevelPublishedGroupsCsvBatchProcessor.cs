using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Polly;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class ChannelLevelPublishedGroupsCsvBatchProcessor : CsvBatchProcessBase, IFundingLineCsvBatchProcessor
    {
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly AsyncPolicy _publishedFundingPolicy;
        private readonly IReleaseManagementRepository _repo;

        public ChannelLevelPublishedGroupsCsvBatchProcessor(IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies resiliencePolicies,
            IFileSystemAccess fileSystemAccess,
            ICsvUtils csvUtils,
            IReleaseManagementRepository repo) : base(fileSystemAccess, csvUtils)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));

            _publishedFundingRepository = publishedFundingRepository;
            _publishedFundingPolicy = resiliencePolicies.PublishedFundingRepository;
            _repo = repo;
        }

        public bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return jobType == FundingLineCsvGeneratorJobType.ChannelLevelPublishedGroup;
        }

        public async Task<bool> GenerateCsv(FundingLineCsvGeneratorJobType jobType,
            string specificationId,
            string fundingPeriodId,
            string temporaryFilePath,
            IFundingLineCsvTransform fundingLineCsvTransform,
            string fundingLineName,
            string fundingStreamId,
            string channelCode)
        {
            bool outputHeaders = true;
            bool processedResults = false;
            IEnumerable<FundingGroupVersion> fundingGroupVersions = await _repo.GetFundingGroupVersionsForSpecificationId(specificationId);
            IDictionary<string, bool> outputHeaderEnabingGroup = new Dictionary<string, bool>();
            await _publishedFundingPolicy.ExecuteAsync(() => _publishedFundingRepository.PublishedGroupBatchProcessing(
                specificationId,
                async publishedFundings =>
                {
                    List<PublishedFundingWithProvider> publishedfundingsWithProviders = new List<PublishedFundingWithProvider>();
                    IDictionary<string, List<PublishedFundingWithProvider>> channelBasedPublishedFundingWithProviders = new Dictionary<string, List<PublishedFundingWithProvider>>();
                    foreach (PublishedFunding publishedFunding in publishedFundings)
                    {
                        IEnumerable<PublishedProvider> providers = Enumerable.Empty<PublishedProvider>();

                        if (publishedFunding.Current.ProviderFundings.Any())
                        {
                            foreach (IEnumerable<string> fundingIds in publishedFunding.Current.ProviderFundings.ToBatches(100))
                            {
                                providers = providers.Concat(await _publishedFundingRepository.QueryPublishedProvider(specificationId, fundingIds));
                            }
                        }
                        IEnumerable<FundingGroupVersion> channelBasedfundingGroupVersion = fundingGroupVersions.Where(_=>_.FundingId.Equals(publishedFunding.Current.FundingId));
                        channelBasedfundingGroupVersion.ForEach(_ =>
                        {
                            publishedFunding.Current.ChannelVersions = new List<ChannelVersion>() {
                                                                        new ChannelVersion()
                                                                        {
                                                                            type = _.UrlKey,
                                                                            value = _.ChannelVersion
                                                                        }
                                };
                            if (!channelBasedPublishedFundingWithProviders.TryGetValue(_.UrlKey, out List<PublishedFundingWithProvider> outPublishedFundingWithProvider))
                            {
                                outPublishedFundingWithProvider = new List<PublishedFundingWithProvider>();
                                channelBasedPublishedFundingWithProviders.Add(_.UrlKey, outPublishedFundingWithProvider);
                            }
                            outPublishedFundingWithProvider.Add(new PublishedFundingWithProvider { PublishedFunding = publishedFunding, PublishedProviders = providers });
                        });                            
                    }
                    foreach(KeyValuePair<string, List<PublishedFundingWithProvider>> data in channelBasedPublishedFundingWithProviders)
                    {
                        IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(data.Value, jobType);
                        string tempPath = temporaryFilePath.Replace("<channelCode>", data.Key.ToPascalCase());
                        if (!outputHeaderEnabingGroup.TryGetValue(data.Key, out bool outputHeader))
                        {
                            outputHeaders = true;
                            outputHeaderEnabingGroup.Add(data.Key, true);
                        } else
                        {
                            outputHeaders = false;
                        }
                        AppendCsvFragment(tempPath, csvRows, outputHeaders);
                        processedResults = true;
                    }
                }, BatchSize)
            );

            return processedResults;

        }
    }
}