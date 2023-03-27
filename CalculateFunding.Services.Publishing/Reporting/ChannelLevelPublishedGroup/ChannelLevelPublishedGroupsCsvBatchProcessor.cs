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
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Polly;
using CalculateFunding.Common.ApiClient.Models;
using System;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Services.Core.Extensions;
using Newtonsoft.Json;
using static Dapper.SqlMapper;
using System.Reflection.Metadata;
using Microsoft.Azure.Search.Common;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class ChannelLevelPublishedGroupsCsvBatchProcessor : CsvBatchProcessBase, IFundingLineCsvBatchProcessor
    {
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IProvidersApiClient _providersApiClient;

        private readonly IReleaseManagementRepository _repo;

        public ChannelLevelPublishedGroupsCsvBatchProcessor(
            IPublishingResiliencePolicies resiliencePolicies,
            IFileSystemAccess fileSystemAccess,
            ICsvUtils csvUtils,
            IReleaseManagementRepository repo,
            ISpecificationsApiClient specificationsApiClient,
            IProvidersApiClient providersApiClient) : base(fileSystemAccess, csvUtils)
        {
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(providersApiClient, nameof(providersApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.ProvidersApiClient, nameof(resiliencePolicies.ProvidersApiClient));

            _repo = repo;
            _specificationsApiClient = specificationsApiClient;
            _providersApiClient = providersApiClient;
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

            IEnumerable<PublishedFundingChannelVersion> fundingChannelVersions = await _repo.GetChannelPublishedFundingGroupsForSpecificationId(specificationId);

            var specApiResponse = await _specificationsApiClient.GetSpecificationSummaryById(specificationId);
            if (specApiResponse.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"Failed to retrieve specification summary {specificationId} from API");

            var providerVersionApiResponse = await _providersApiClient.GetProvidersByVersion(specApiResponse.Content.ProviderVersionId);
            if (providerVersionApiResponse.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"Failed to retrieve provider version {specApiResponse.Content.ProviderVersionId} from API");
            var providers = providerVersionApiResponse.Content.Providers.ToDictionary(x=> x.ProviderId);

            var publishedFundingGroups = fundingChannelVersions.GroupBy(x=> x.ProviderId).ToDictionary(g => g.Key, g => g.ToList());
            publishedFundingGroups.ForEach(group => 
            {
                if (!providers.ContainsKey(group.Key))
                {
                    group.Value.ForEach(y =>
                    {
                        y.ProviderName = "Provider details not available from current providers snapshot";
                        y.ProviderUKPRN = y.ProviderId;
                    });
                }
                else
                {
                    var provider = providers[group.Key];
                    group.Value.ForEach(y => 
                    {
                        y.ProviderName = provider.Name;
                        y.ProviderUKPRN = provider.UKPRN;
                        y.ProviderURN = provider.URN;
                        y.ProviderUPIN = provider.UPIN;
                        y.ProviderLACode = provider.LACode;
                        y.ProviderStatus = provider.Status;
                        y.ProviderSuccessor = provider.Successor;
                        y.ProviderPredecessors = provider.Predecessors.Join("|");
                    });
                }

            });

            IDictionary<string, List<PublishedFundingChannelVersion>> channelBasedPublishedFundingWithProviders = fundingChannelVersions.GroupBy(o => o.ChannelCode).ToDictionary(g => g.Key, g => g.ToList());

            IDictionary<string, bool> outputHeaderEnabingGroup = new Dictionary<string, bool>();
            foreach (KeyValuePair<string, List<PublishedFundingChannelVersion>> data in channelBasedPublishedFundingWithProviders)
            {
                var providersCount = data.Value.GroupBy(_ => _.FundingId).Select(x => new { FundingId = x.Key, Count = x.Count() }).ToList();
                var fundingGroups = data.Value.GroupBy(_ => _.FundingId).ToDictionary(g => g.Key, g => g.ToList());
                providersCount.ForEach(x => {
                    fundingGroups[x.FundingId].ForEach(y => { y.ProviderCount = x.Count; });
                });

                IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(data.Value, jobType);
                string tempPath = temporaryFilePath.Replace("<channelCode>", data.Key.ToPascalCase());
                if (!outputHeaderEnabingGroup.TryGetValue(data.Key, out bool outputHeader))
                {
                    outputHeaders = true;
                    outputHeaderEnabingGroup.Add(data.Key, true);
                }
                else
                {
                    outputHeaders = false;
                }
                AppendCsvFragment(tempPath, csvRows, outputHeaders);
                processedResults = true;
            }

            return processedResults;

        }
    }
}