using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using System.Collections.Generic;

namespace CalculateFunding.Api.Publishing.IntegrationTests.Data
{
    public class FundingConfigurationTemplateParameters
    {
        public string Id { get; set; }

        public string FundingStreamId { get; set; }

        public string FundingPeriodId { get; set; }

        public string DefaultTemplateVersion { get; set; }

        public string[] IndicativeOpenerProviderStatus { get; set; }

        public bool EnableConverterDataMerge { get; set; }

        public ProviderSource ProviderSource { get; set; }

        public string[] AllowedPublishedFundingStreamsIdsToReference { get; set; }

        public IEnumerable<FundingConfigurationChannel> ReleaseChannels { get; set; }
    }
}