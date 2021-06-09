using CalculateFunding.Common.ApiClient.Models;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
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
    }
}