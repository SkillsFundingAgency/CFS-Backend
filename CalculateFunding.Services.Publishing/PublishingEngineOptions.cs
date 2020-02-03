using System.Runtime.CompilerServices;
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

        public int GetCalculationResultsConcurrencyCount => GetPublishEngineOptionsConfigurationValue();

        public int SavePublishedProviderContentsConcurrencyCount => GetPublishEngineOptionsConfigurationValue();

        public int SavePublishedFundingContentsConcurrencyCount => GetPublishEngineOptionsConfigurationValue();

        public int GetPublishedProvidersForApprovalConcurrencyCount => GetPublishEngineOptionsConfigurationValue();

        public int GetCurrentPublishedFundingConcurrencyCount => GetPublishEngineOptionsConfigurationValue();

        public int GetCurrentPublishedProvidersConcurrencyCount => GetPublishEngineOptionsConfigurationValue();

        public int UpdatePublishedFundingStatusConcurrencyCount => GetPublishEngineOptionsConfigurationValue();

        public int IndexPublishedProvidersConcurrencyCount => GetPublishEngineOptionsConfigurationValue();

        public int CreateLatestPublishedProviderVersionsConcurrencyCount => GetPublishEngineOptionsConfigurationValue();
        
        public int PublishedProviderCreateVersionsConcurrencyCount => GetPublishEngineOptionsConfigurationValue();

        public int PublishedProviderSaveVersionsConcurrencyCount => GetPublishEngineOptionsConfigurationValue();

        // ReSharper disable once StringLiteralTypo
        private int GetPublishEngineOptionsConfigurationValue([CallerMemberName] string key = null) => int.TryParse(_configuration[$"publishingengineoptions:{key}"],
            out var intValue)
            ? intValue
            : DefaultConcurrencyCount;
    }
}