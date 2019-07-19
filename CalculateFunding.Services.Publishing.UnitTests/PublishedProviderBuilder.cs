using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.UnitTests.Repositories;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedProviderBuilder : TestEntityBuilder
    {
        private PublishedProviderVersion _current;

        public PublishedProviderBuilder WithCurrent(PublishedProviderVersion current)
        {
            _current = current;

            return this;
        }

        public PublishedProvider Build()
        {
            return new PublishedProvider
            {
                Current = _current ?? new PublishedProviderVersionBuilder()
                              .Build()
            };
        }
    }
}