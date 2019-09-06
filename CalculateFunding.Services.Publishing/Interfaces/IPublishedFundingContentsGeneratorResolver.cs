namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingContentsGeneratorResolver
    {
        IPublishedFundingContentsGenerator GetService(string schemaVersion);

        bool TryGetService(string schemaVersion, out IPublishedFundingContentsGenerator publishedFundingContentsGenerator);

        bool Contains(string schemaVersion);

        void Register(string schemaVersion, IPublishedFundingContentsGenerator publishedFundingContentsGenerator);
    }
}
