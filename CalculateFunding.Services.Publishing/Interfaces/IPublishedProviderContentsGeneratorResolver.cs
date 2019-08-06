namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderContentsGeneratorResolver
    {
        IPublishedProviderContentsGenerator GetService(string schemaVersion);

        bool TryGetService(string schemaVersion, out IPublishedProviderContentsGenerator publishedProviderContentsGenerator);

        bool Contains(string schemaVersion);

        void Register(string schemaVersion, IPublishedProviderContentsGenerator publishedProviderContentsGenerator);
    }
}
