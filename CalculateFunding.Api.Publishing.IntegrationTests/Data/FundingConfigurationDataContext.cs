using System.Reflection;
using System.Text.Json;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.Extensions;
using CalculateFunding.IntegrationTests.Common.Data;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Api.Publishing.IntegrationTests.Data
{
    public class FundingConfigurationDataContext : CosmosBulkDataContext
    {
        public FundingConfigurationDataContext(IConfiguration configuration,
            Assembly resourceAssembly) : base(configuration,
            "policy",
            "CalculateFunding.Api.Publishing.IntegrationTests.Resources.FundingConfigurationTemplate",
            resourceAssembly)
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now) =>
            new
            {
                ID = $"config-{documentData.FundingStreamId}-{documentData.FundingPeriodId}",
                FUNDINGSTREAMID = documentData.FundingStreamId,
                FUNDINGPERIODID = documentData.FundingPeriodId,
                DEFAULTTEMPLATEVERSION = documentData.DefaultTemplateVersion,
                INDICATIVEOPENERPROVIDERSTATUS = ((string[]) documentData.IndicativeOpenerProviderStatus).AsJson(),
                ENABLECONVERTERDATAMERGE = documentData.EnableConverterDataMerge.ToString().ToLower(),
                PROVIDERSOURCE = documentData.ProviderSource.ToString(),
                NOW = now,
                ALLOWEDPUBLISHEDFUNDINGSTREAMIDSTOREFERENCE = ((string[])documentData.AllowedPublishedFundingStreamsIdsToReference).AsJson(),
                RELEASECHANNELS = ((FundingConfigurationChannel[])documentData.ReleaseChannels).AsJson(),
            };

        protected override string GetPartitionKey(JsonElement content)
            => GetId(content);
    }
}