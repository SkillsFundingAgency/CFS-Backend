using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedFundingBuilder : TestEntityBuilder
    {
        private PublishedFundingVersion _current;

        public PublishedFundingBuilder WithCurrent(PublishedFundingVersion current)
        {
            _current = current;

            return this;
        }

        public PublishedFunding Build()
        {
            return new PublishedFunding
            {
                Current = _current ?? new PublishedFundingVersionBuilder()
                              .Build()
            };
        }
    }
}