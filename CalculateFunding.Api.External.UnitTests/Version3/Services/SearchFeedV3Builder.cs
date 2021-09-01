using System.Collections.Generic;
using CalculateFunding.Models.External;
using CalculateFunding.Models.Search;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.External.UnitTests.Version3.Services
{
    public class SearchFeedV3Builder<TFeedItem> : TestEntityBuilder
        where TFeedItem : class
    {
        private IEnumerable<TFeedItem> _feedItems;

        public SearchFeedV3Builder<TFeedItem> WithFeedItems(params TFeedItem[] feedItems)
        {
            _feedItems = feedItems;

            return this;
        }

        public SearchFeedResult<TFeedItem> Build()
        {
            return new SearchFeedResult<TFeedItem>
            {
                Entries = _feedItems ?? new TFeedItem[0]
            };
        }
    }
}