using System.Runtime.CompilerServices;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Services.Publishing
{
    public class PublishingEngineOptions : IPublishingEngineOptions
    {
        private const int DefaultInt = 15;
        private const int DefaultMaxRequestsPerTcpConnection = 8;
        private const int DefaultMaxTcpConnectionsPerEndpoint = 4;
        private const int DefaultMaxBatchSize = 10;

        private readonly IConfiguration _configuration;

        public PublishingEngineOptions(IConfiguration configuration)
        {
            Guard.ArgumentNotNull(configuration, nameof(configuration));
            
            _configuration = configuration;
        }

        public int GetCalculationResultsConcurrencyCount => GetPublishEngineOptionsConfigurationInteger();

        public int SavePublishedProviderContentsConcurrencyCount => GetPublishEngineOptionsConfigurationInteger();

        public int SavePublishedFundingContentsConcurrencyCount => GetPublishEngineOptionsConfigurationInteger();

        public int GetPublishedProvidersForApprovalConcurrencyCount => GetPublishEngineOptionsConfigurationInteger();

        public int GetCurrentPublishedFundingConcurrencyCount => GetPublishEngineOptionsConfigurationInteger();

        public int GetCurrentPublishedProvidersConcurrencyCount => GetPublishEngineOptionsConfigurationInteger();

        public int UpdatePublishedFundingStatusConcurrencyCount => GetPublishEngineOptionsConfigurationInteger();

        public int IndexPublishedProvidersConcurrencyCount => GetPublishEngineOptionsConfigurationInteger();
        public int ProfilingPublishedProvidersConcurrencyCount => GetPublishEngineOptionsConfigurationInteger();

        public int CreateLatestPublishedProviderVersionsConcurrencyCount => GetPublishEngineOptionsConfigurationInteger();
        
        public int PublishedProviderCreateVersionsConcurrencyCount => GetPublishEngineOptionsConfigurationInteger();

        public int PublishedProviderSaveVersionsConcurrencyCount => GetPublishEngineOptionsConfigurationInteger();

        public int PublishedFundingConcurrencyCount => GetPublishEngineOptionsConfigurationInteger();

        public int MaxRequestsPerTcpConnectionPublishedFundingCosmosBulkOptions => GetPublishEngineOptionsConfigurationInteger(overrideDefaultValue: DefaultMaxRequestsPerTcpConnection);
        
        public int MaxTcpConnectionsPerEndpointPublishedFundingCosmosBulkOptions => GetPublishEngineOptionsConfigurationInteger(overrideDefaultValue: DefaultMaxTcpConnectionsPerEndpoint);

        public int MaxRequestsPerTcpConnectionCalculationsCosmosBulkOptions => GetPublishEngineOptionsConfigurationInteger(overrideDefaultValue: DefaultMaxRequestsPerTcpConnection);
        
        public int MaxTcpConnectionsPerEndpointCalculationsCosmosBulkOptions => GetPublishEngineOptionsConfigurationInteger(overrideDefaultValue: DefaultMaxTcpConnectionsPerEndpoint);

        public int MaxBatchSizePublishedFunding => GetPublishEngineOptionsConfigurationInteger(overrideDefaultValue: DefaultMaxBatchSize);

        public bool AllowBatching => GetPublishEngineOptionsConfigurationBool();

        // ReSharper disable once StringLiteralTypo
        private int GetPublishEngineOptionsConfigurationInteger([CallerMemberName] string key = null, int? overrideDefaultValue = null) => int.TryParse(_configuration[$"publishingengineoptions:{key}"],
            out int intValue)
            ? intValue
            : overrideDefaultValue ?? DefaultInt;

        private bool GetPublishEngineOptionsConfigurationBool([CallerMemberName] string key = null, bool defaultValue = false) => bool.TryParse(_configuration[$"publishingengineoptions:{key}"],
            out bool boolValue)
            ? boolValue
            : defaultValue;
    }
}