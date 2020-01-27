using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedProviderBuilder : TestEntityBuilder
    {
        private PublishedProviderVersion _current;
        private PublishedProviderVersion _released;
        private bool _noCurrent;

        public PublishedProviderBuilder WithCurrent(PublishedProviderVersion current)
        {
            _current = current;

            return this;
        }

        public PublishedProviderBuilder WithReleased(PublishedProviderVersion released)
        {
            _released = released;

            return this;
        }

        public PublishedProviderBuilder WithNoCurrent()
        {
            _noCurrent = true;

            return this;
        }

        public PublishedProvider Build()
        {
            return new PublishedProvider
            {
                Released =  _released,
                Current = _current ?? (_noCurrent ? null : new PublishedProviderVersionBuilder()
                              .Build())
            };
        }
    }
}