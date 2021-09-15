using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    public class ChannelBuilder : TestEntityBuilder
    {
        private int? _channelId;
        private string _channelCode;
        private string _channelName;
        private string _urlKey;

        public ChannelBuilder WithChannelId(int channelId)
        {
            _channelId = channelId;
            return this;
        }

        public ChannelBuilder WithChannelCode(string channelCode)
        {
            _channelCode = channelCode;
            return this;
        }

        public ChannelBuilder WithChannelName(string channelName)
        {
            _channelName = channelName;
            return this;
        }

        public ChannelBuilder WithUrlKey(string urlKey)
        {
            _urlKey = urlKey;
            return this;
        }

        public Channel Build()
            => new Channel
            {
                ChannelId = _channelId ?? NewRandomNumberBetween(1, 1000),
                ChannelCode = _channelCode ?? NewRandomString(),
                ChannelName = _channelName ?? NewRandomString(),
                UrlKey = _urlKey ?? NewRandomString(),
            };
    }
}
