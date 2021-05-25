using System.Reflection;
using System.Text.Json;
using CalculateFunding.Common.Extensions;
using CalculateFunding.IntegrationTests.Common.Data;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Data
{
    public class FundingConfigurationDataContext : CosmosBulkDataContext
    {
        public FundingConfigurationDataContext(IConfiguration configuration,
            Assembly resourceAssembly) : base(configuration,
            "policy",
            "CalculateFunding.Api.Datasets.IntegrationTests.Resources.FundingConfigurationTemplate",
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
                NOW = now
            };

        protected override string GetPartitionKey(JsonElement content)
            => GetId(content);
    }
}