using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Services.Publishing
{
    public class PublishingEngineOptions : IPublishingEngineOptions
    {
        private const int DefaultConcurrencyCount = 15;
        private readonly IConfiguration _configuration;

        public PublishingEngineOptions(IConfiguration configuration)
        {
            Guard.ArgumentNotNull(configuration, nameof(configuration));
            _configuration = configuration;
        }

        public int GetCalculationResultsConcurrencyCount =>
            int.TryParse(_configuration["publishingengineoptions:GetCalculationResultsConcurrencyCount"],
                out var intValue)
                ? intValue
                : DefaultConcurrencyCount;

        public int SavePublishedProviderContentsConcurrencyCount =>
            int.TryParse(_configuration["publishingengineoptions:SavePublishedProviderContentsConcurrencyCount"],
                out var intValue)
                ? intValue
                : DefaultConcurrencyCount;

        public int SavePublishedFundingContentsConcurrencyCount =>
            int.TryParse(_configuration["publishingengineoptions:SavePublishedFundingContentsConcurrencyCount"],
                out var intValue)
                ? intValue
                : DefaultConcurrencyCount;

        public int GetPublishedProvidersForApprovalConcurrencyCount =>
            int.TryParse(_configuration["publishingengineoptions:GetPublishedProvidersForApprovalConcurrencyCount"],
                out var intValue)
                ? intValue
                : DefaultConcurrencyCount;

        public int GetCurrentPublishedFundingConcurrencyCount =>
            int.TryParse(_configuration["publishingengineoptions:GetCurrentPublishedFundingConcurrencyCount"],
                out var intValue)
                ? intValue
                : DefaultConcurrencyCount;

        public int GetCurrentPublishedProvidersConcurrencyCount =>
            int.TryParse(_configuration["publishingengineoptions:GetCurrentPublishedProvidersConcurrencyCount"],
                out var intValue)
                ? intValue
                : DefaultConcurrencyCount;

        public int UpdatePublishedFundingStatusConcurrencyCount =>
            int.TryParse(_configuration["publishingengineoptions:UpdatePublishedFundingStatusConcurrencyCount"],
                out var intValue)
                ? intValue
                : DefaultConcurrencyCount;

        public int IndexPublishedProvidersConcurrencyCount =>
            int.TryParse(_configuration["publishingengineoptions:IndexPublishedProvidersConcurrencyCount"],
                out var intValue)
                ? intValue
                : 3;

        public int CreateLatestPublishedProviderVersionsConcurrencyCount =>
            int.TryParse(_configuration["publishingengineoptions:CreateLatestPublishedProviderVersionsConcurrencyCount"],
                out var intValue)
                ? intValue
                : DefaultConcurrencyCount;

        public int PublishedProviderCreateVersionsConcurrencyCount =>
            int.TryParse(_configuration["publishingengineoptions:PublishedProviderCreateVersionsConcurrencyCount"],
                out var intValue)
                ? intValue
                : DefaultConcurrencyCount;

        public int PublishedProviderSaveVersionsConcurrencyCount =>
            int.TryParse(_configuration["publishingengineoptions:PublishedProviderSaveVersionsConcurrencyCount"],
                out var intValue)
                ? intValue
                : DefaultConcurrencyCount;
    }
}