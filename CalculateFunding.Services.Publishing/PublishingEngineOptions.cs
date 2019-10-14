using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishingEngineOptions : IPublishingEngineOptions
    {
        private const int DefaultConcurrencyCount = 15;
        
        public int GetCalculationResultsConcurrencyCount { get; set; } = DefaultConcurrencyCount;

        public int SavePublishedProviderContentsConcurrencyCount { get; set; } = DefaultConcurrencyCount;

        public int SavePublishedFundingContentsConcurrencyCount { get; set; } = DefaultConcurrencyCount;
        
        public int GetPublishedProvidersForApprovalConcurrencyCount { get; set; } = DefaultConcurrencyCount;
        
        public int GetCurrentPublishedFundingConcurrencyCount { get; set; } = DefaultConcurrencyCount;
        
        public int GetCurrentPublishedProvidersConcurrencyCount { get; set; } = DefaultConcurrencyCount;
        
        public int UpdatePublishedFundingStatusConcurrencyCount { get; set; } = DefaultConcurrencyCount;
        
        public int IndexPublishedProvidersConcurrencyCount { get; set; } = 3;
        
        public int CreateLatestPublishedProviderVersionsConcurrencyCount { get; set; } = DefaultConcurrencyCount;
        
        public int PublishedProviderCreateVersionsConcurrencyCount { get; set; } = DefaultConcurrencyCount;
        
        public int PublishedProviderSaveVersionsConcurrencyCount { get; set; } = DefaultConcurrencyCount;
    }
}