using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderStatusUpdateSettings : IPublishedProviderStatusUpdateSettings
    {
        public int BatchSize { get; set; } = 200;
    }
}