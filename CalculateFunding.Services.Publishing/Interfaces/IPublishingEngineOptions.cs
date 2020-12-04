namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishingEngineOptions
    {
        int GetCalculationResultsConcurrencyCount { get; }
        
        int SavePublishedProviderContentsConcurrencyCount { get; }
        
        int SavePublishedFundingContentsConcurrencyCount { get; }
        
        int GetPublishedProvidersForApprovalConcurrencyCount { get; }
        
        int GetCurrentPublishedFundingConcurrencyCount { get; }
        
        int GetCurrentPublishedProvidersConcurrencyCount { get; }
        
        int UpdatePublishedFundingStatusConcurrencyCount { get; }
        
        int IndexPublishedProvidersConcurrencyCount { get; }

        int ProfilingPublishedProvidersConcurrencyCount { get; }

        int CreateLatestPublishedProviderVersionsConcurrencyCount { get; }
        
        int PublishedProviderCreateVersionsConcurrencyCount { get; }
        
        int PublishedProviderSaveVersionsConcurrencyCount { get; }
    }
}